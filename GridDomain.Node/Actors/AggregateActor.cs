using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DI.Core;
using Akka.Util.Internal;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.CQRS.Messaging;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.CommonDomain;
using GridDomain.EventSourcing.FutureEvents;
using GridDomain.EventSourcing.Sagas.InstanceSagas;
using GridDomain.Logging;
using GridDomain.Node.Actors.CommandPipe;
using GridDomain.Node.AkkaMessaging;
using GridDomain.Scheduling.Akka.Messages;
using Serilog;

namespace GridDomain.Node.Actors
{
    /// <summary>
    ///     Name should be parse by AggregateActorName
    /// </summary>
    /// <typeparam name="TAggregate"></typeparam>
    public class AggregateActor<TAggregate> : ReceiveActor where TAggregate : Aggregate
//EventSourcedActor<TAggregate> where TAggregate : Aggregate
    {
        private readonly IActorRef _customHandlersActor;
        private readonly ProcessEntry _domainEventProcessEntry;
        private readonly ProcessEntry _domainEventProcessFailEntry;
        private readonly IPublisher _publisher;
        private readonly IActorRef _schedulerActorRef;

        private readonly IAggregateCommandsHandler<TAggregate> _aggregateCommandsHandler;
        private readonly List<IActorRef> _commandCompletedWaiters = new List<IActorRef>();
        private AggregateCommandExecutionContext<TAggregate> ExecutionContext { get; } = new AggregateCommandExecutionContext<TAggregate>();
        protected readonly BehaviorStack Behavior;
        public TAggregate State { get; set; }
        protected Guid Id { get; }
        protected readonly ActorMonitor Monitor;
        private IActorRef _stateActor;
        private readonly IConstructAggregates _aggregateConstructor;

        public AggregateActor(IAggregateCommandsHandler<TAggregate> handler,
                              IActorRef schedulerActorRef,
                              IPublisher publisher,
                              IConstructAggregates aggregateConstructor,
                              IActorRef customHandlersActor)
        {
            _aggregateConstructor = aggregateConstructor;
            _aggregateCommandsHandler = handler;
            _publisher = publisher;
            _customHandlersActor = customHandlersActor;
            _schedulerActorRef = schedulerActorRef;
            _domainEventProcessEntry = new ProcessEntry(Self.Path.Name, SimpleAggregateActorConstants.PublishingEvent, SimpleAggregateActorConstants.CommandExecutionCreatedAnEvent);
            _domainEventProcessFailEntry = new ProcessEntry(Self.Path.Name, SimpleAggregateActorConstants.CreatedFault, SimpleAggregateActorConstants.CommandRaisedAnError);

            Behavior.Become(AwaitingCommandBehavior, nameof(AwaitingCommandBehavior));
            Guid id;
            if(!AggregateActorName.TryParseId(Self.Path.Name, out id))
                throw new BadNameFormatException();
            Id = id;

            Behavior = new BehaviorStack(BecomeStacked, UnbecomeStacked);
            State = (TAggregate)aggregateConstructor.Build(typeof(TAggregate), Id, null);
            Monitor = new ActorMonitor(Context, typeof(TAggregate).BeautyName());

            var stateActorProps = Context.DI().Props(typeof(EventSourcedActor<TAggregate>));
            _stateActor = Context.ActorOf(stateActorProps, AggregateActorName.New<TAggregate>(Id).Name);

            //_stateAggregateActor = _stateActor;
            //_stateAggregateActor.Tell(NotifyOnCommandComplete.Instance);
            _stateActor.Tell(GetState.Instance);

            Behavior.Become(InitializingBehavior, nameof(InitializingBehavior));
        }

        private void InitializingBehavior()
        {
            DefaultBehavior();
            Receive<AggregateState>(s =>
                                    {
                                        if (s.Snapshot != null)
                                            State = (TAggregate) _aggregateConstructor.Build(typeof(TAggregate),
                                                Id,
                                                s.Snapshot);
                                        s.Events.ForEach(e => ((IAggregate) State).ApplyEvent(e));
                                        Behavior.Become(AwaitingCommandBehavior,nameof(AwaitingCommandBehavior));
                                    });
        }

        private void DefaultBehavior()
        {
            Receive<NotifyOnCommandComplete>(n =>
                                             {
                                                 _commandCompletedWaiters.Add(Sender);
                                                 Sender.Tell(NotifyOnCommandCompletedAck.Instance);
                                             });
        }
        protected virtual void AwaitingCommandBehavior()
        {
            DefaultBehavior();

            ReceiveAsync<IMessageMetadataEnvelop<ICommand>>(m =>
                                                       {
                                                           var cmd = m.Message;
                                                           Monitor.IncrementMessagesReceived();
                                                           Log.Debug("{Aggregate} received a {@command}", PersistenceId, cmd);
                                                           ExecutionContext.Command = cmd;
                                                           ExecutionContext.CommandMetadata = m.Metadata;
                                                           await _aggregateCommandsHandler.ExecuteAsync(State, cmd)
                                                                                          .ContinueWith(t =>
                                                                                                       {
                                                                                                           ExecutionContext.ProducedState = t.Result;
                                                                                                           return EventPersistingInProgress.Instance;
                                                                                                       })
                                                                                    .PipeTo(Self);

                                                           Behavior.Become(ProcessingCommandBehavior, nameof(ProcessingCommandBehavior));
                                                       });
        }

        private void ProcessingCommandBehavior()
        {
            var commandMetadata = ExecutionContext.CommandMetadata;
            var producedEventsMetadata = commandMetadata.CreateChild(Id, _domainEventProcessEntry);
            
            Command<EventPersistingInProgress>(e =>
                                               {
                                                   RegisterAggregatePersistence(ExecutionContext.ProducedState);

                                                   ICommand command = ExecutionContext.Command;
                                                   var domainEvents = ExecutionContext.ProducedState.GetDomainEvents();
                                                   ExecutionContext.MessagesToProject = domainEvents;

                                                   if (!domainEvents.Any())
                                                   {
                                                       Log.Warning("Aggregate {id} is saving zero events", PersistenceId);
                                                   }

                                                   PersistAll(domainEvents.Select(evt => evt.CloneWithSaga(command.SagaId)),
                                                       persistedEvent =>
                                                       {
                                                           try
                                                           {
                                                               ExecutionContext.ProducedState.MarkPersisted(persistedEvent);
                                                           }
                                                           catch (Exception ex)
                                                           {
                                                               Log.Error(SimpleAggregateActorConstants.ErrorOnEventApplyText, Id, command);
                                                               PublishError(command, ExecutionContext.CommandMetadata, ex);
                                                               //intentionally drop all pending commands and messages
                                                               //and don't wait end of projection builders processing as
                                                               //state is corrupted
                                                               Context.Stop(Self);
                                                               return;
                                                           }

                                                           NotifyPersistenceWatchers(persistedEvent);
                                                           TrySaveSnapshot(ExecutionContext.ProducedState);

                                                           if (State.HasUncommitedEvents)
                                                               return;

                                                           Task persistenceFinishedTask = null;
                                                           try
                                                           {
                                                               persistenceFinishedTask = ExecutionContext.AfterPersistAction();
                                                           }
                                                           catch (Exception ex)
                                                           {
                                                               Log.Error(SimpleAggregateActorConstants.ErrorOnContinuationText, Id, command);
                                                               persistenceFinishedTask = PublishError(command, ExecutionContext.CommandMetadata, ex);
                                                           }
                                                           finally
                                                           {
                                                               persistenceFinishedTask.ContinueWith(t => ProducedEventsPersisted.Instance).
                                                                                       PipeTo(Self);
                                                           }
                                                       });
                                               });
            //aggregate raised an error during command execution
            Command<Status.Failure>(f => PublishError(ExecutionContext.Command, commandMetadata, f.Cause).PipeTo(Self));

            Command<ProducedEventsPersisted>(newState =>
                                             {
                                                 ExecutionContext.MessagesToProject
                                                                 .Select(e => Project(e, producedEventsMetadata)).
                                                                  ToChain().
                                                                  ContinueWith(t =>
                                                                               {
                                                                                   //Publish produced messages
                                                                                   foreach (var e in ExecutionContext.MessagesToProject)
                                                                                       _publisher.Publish(e, producedEventsMetadata);
                                                                                   return CommandExecuted.Instance;
                                                                               });
                                             });

            Command<CommandExecuted>(c =>
                                     {
                                         //finish command execution
                                         State = ExecutionContext.ProducedState;
                                         ExecutionContext.Clear();

                                         Behavior.Unbecome();
                                         Stash.UnstashAll();
                                         //notify waiters
                                         foreach (var waiter in _commandCompletedWaiters)
                                             waiter.Tell(new CommandCompleted(ExecutionContext.Command.Id));
                                     });

            Command<IMessageMetadataEnvelop<ICommand>>(o => StashMessage(o));

            Command<GracefullShutdownRequest>(o => StashMessage(o));

            DefaultBehavior();
        }

        private Task<AllHandlersCompleted> PublishError(ICommand command, IMessageMetadata commandMetadata, Exception exception)
        {
            var producedFaultMetadata = commandMetadata.CreateChild(command.Id, _domainEventProcessFailEntry);

            var fault = Fault.NewGeneric(command, exception, command.SagaId, typeof(TAggregate));
            Log.Error(exception, "{Aggregate} raised an error {@Exception} while executing {@Command}", PersistenceId, exception, command);

            return Project(fault, producedFaultMetadata).
                ContinueWith(t =>
                             {
                                 _publisher.Publish(fault, producedFaultMetadata);
                                 return t.Result;
                             });
        }

        protected override void TerminatingBehavior()
        {
            Command<IMessageMetadataEnvelop<ICommand>>(c =>
                                                       {
                                                           Self.Tell(CancelShutdownRequest.Instance);
                                                           StashMessage(c);
                                                       });
            base.TerminatingBehavior();
        }

        private Task<AllHandlersCompleted> Project(object evt, IMessageMetadata commandMetadata)
        {
            var envelop = new MessageMetadataEnvelop<Project>(new Project(evt), commandMetadata);

            switch (evt)
            {
                case FutureEventScheduledEvent e:
                    Handle(e, commandMetadata);
                    break;
                case FutureEventCanceledEvent e:
                    Handle(e, commandMetadata);
                    break;
            }

            return _customHandlersActor.Ask<AllHandlersCompleted>(envelop);
        }

        private Task Handle(FutureEventScheduledEvent futureEventScheduledEvent, IMessageMetadata messageMetadata)
        {
            var message = futureEventScheduledEvent;
            var scheduleId = message.Id;
            var aggregateId = message.Event.SourceId;

            var description = $"Aggregate {typeof(TAggregate).Name} id = {aggregateId} scheduled future event "
                              + $"{scheduleId} with payload type {message.Event.GetType(). Name} on time {message.RaiseTime}\r\n"
                              + $"Future event: {message.ToPropsString()}";

            var scheduleKey = CreateScheduleKey(scheduleId, aggregateId, description);

            var command = new RaiseScheduledDomainEventCommand(message.Id, message.SourceId, Guid.NewGuid());
            var metadata = messageMetadata.CreateChild(command.Id,
                                                       new ProcessEntry(GetType().
                                                                            Name,
                                                                        "Scheduling raise future event command",
                                                                        "FutureEventScheduled event occured"));
            var scheduleEvent = new ScheduleCommand(command,
                                                    scheduleKey,
                                                    ExecutionOptions.ForCommand(message.RaiseTime,
                                                                                message.Event.GetType()),
                                                    metadata);

            return _schedulerActorRef.Ask<Scheduled>(scheduleEvent);
        }

        public static ScheduleKey CreateScheduleKey(Guid scheduleId, Guid aggregateId, string description)
        {
            return new ScheduleKey(scheduleId,
                                   $"{typeof(TAggregate).Name}_{aggregateId}_future_event_{scheduleId}",
                                   $"{typeof(TAggregate).Name}_futureEvents",
                                   "");
        }

        private Task Handle(FutureEventCanceledEvent futureEventCanceledEvent, IMessageMetadata metadata)
        {
            var message = futureEventCanceledEvent;
            var key = CreateScheduleKey(message.FutureEventId, message.SourceId, "");
            var unscheduleMessage = new Unschedule(key);
            return _schedulerActorRef.Ask<Unscheduled>(unscheduleMessage);
        }
    }
}