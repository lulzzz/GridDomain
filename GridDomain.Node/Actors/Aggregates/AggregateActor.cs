using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using Akka.Actor;
using Automatonymous;
using Google.Protobuf.WellKnownTypes;
using GridDomain.Common;
using GridDomain.Configuration;
using GridDomain.Configuration.MessageRouting;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.CommonDomain;
using GridDomain.Node.Actors.Aggregates.Messages;
using GridDomain.Node.Actors.CommandPipe.Messages;
using GridDomain.Node.Actors.EventSourced;
using GridDomain.Node.Actors.EventSourced.Messages;
using GridDomain.Node.Actors.ProcessManagers.Messages;
using GridDomain.Transport.Extension;

namespace GridDomain.Node.Actors.Aggregates
{
    public class CommandAlreadyExecutedException:Exception { }

    /// <summary>
    ///     Name should be parse by AggregateActorName
    /// </summary>
    /// <typeparam name="TAggregate"></typeparam>
    public class AggregateActor<TAggregate> : DomainEventSourcedActor<TAggregate> where TAggregate : class,IAggregate
    {
        
        private readonly IActorRef _customHandlersActor;
        private readonly ProcessEntry _domainEventProcessEntry;
        private readonly ProcessEntry _domainEventProcessFailEntry;
        private readonly ProcessEntry _commandCompletedProcessEntry;
        private readonly IPublisher _publisher;

        private readonly IAggregateCommandsHandler<TAggregate> _aggregateCommandsHandler;
        private AggregateCommandExecutionContext ExecutionContext { get; } = new AggregateCommandExecutionContext();

        public AggregateActor(IAggregateCommandsHandler<TAggregate> handler,
                              ISnapshotsPersistencePolicy snapshotsPersistencePolicy,
                              IConstructAggregates aggregateConstructor,
                              IConstructSnapshots snapshotsConstructor,
                              IActorRef customHandlersActor) : base(aggregateConstructor, snapshotsConstructor, snapshotsPersistencePolicy)
        {
            _aggregateCommandsHandler = handler;
            _publisher = Context.System.GetTransport();
            _customHandlersActor = customHandlersActor;
            _domainEventProcessEntry = new ProcessEntry(Self.Path.Name, AggregateActorConstants.PublishingEvent, AggregateActorConstants.CommandExecutionCreatedAnEvent);
            _domainEventProcessFailEntry = new ProcessEntry(Self.Path.Name, AggregateActorConstants.CommandExecutionFinished, AggregateActorConstants.CommandRaisedAnError);
            _commandCompletedProcessEntry = new ProcessEntry(Self.Path.Name, AggregateActorConstants.CommandExecutionFinished, AggregateActorConstants.ExecutedCommand);
            Behavior.Become(AwaitingCommandBehavior, nameof(AwaitingCommandBehavior));
        }


        protected void ValidatingCommandBehavior()
        {
            DefaultBehavior();
            
            Command<CommandStateActor.Accepted>(a =>
                                                {
                                                    Behavior.Become(ProcessingCommandBehavior, nameof(ProcessingCommandBehavior));

                                                    Log.Debug("Executing command. {@m}", ExecutionContext);
                                                        _aggregateCommandsHandler.ExecuteAsync(State,
                                                                                               ExecutionContext.Command)
                                                                                 .ContinueWith(t =>
                                                                                               {
                                                                                                   ExecutionContext.ProducedState = t.Result;
                                                                                                   return ExecutionContext.ProducedState.GetUncommittedEvents();
                                                                                               })
                                                                                 .PipeTo(Self);
                                                   
                                                });
            Command<CommandStateActor.Rejected>(a =>
                                                {
                                                    var commandAlreadyExecutedException = new CommandAlreadyExecutedException();
                                                    PublishError(commandAlreadyExecutedException);
                                                    Behavior.Become(AwaitingCommandBehavior, nameof(AwaitingCommandBehavior));
                                                   // throw commandAlreadyExecutedException;
                                                    Stash.UnstashAll();
                                                });
            CommandAny(StashMessage);
        }
        
        protected virtual void AwaitingCommandBehavior()
        {
            DefaultBehavior();
            
            Command<IMessageMetadataEnvelop>(m =>
                                            {
                                                Monitor.Increment(nameof(CQRS.Command));
                                                var cmd = (ICommand)m.Message;
                                                var name = cmd.Id.ToString();
                                                Log.Debug($"Received command {cmd.Id}");
                                                var actorRef = Context.Child(name);
                                                ExecutionContext.Validator = actorRef != ActorRefs.Nobody ? actorRef : Context.ActorOf<CommandStateActor>(name);
                                                ExecutionContext.Command = cmd;
                                                ExecutionContext.CommandMetadata = m.Metadata;
                                                ExecutionContext.CommandSender = Sender;
                                                ExecutionContext.Validator.Tell(CommandStateActor.AcceptCommandExecution.Instance);
                                                 
                                                Behavior.Become(ValidatingCommandBehavior,nameof(ValidatingCommandBehavior));
                                                
                                            }, m => m.Message is ICommand);
        }

        protected override bool CanShutdown(out string description)
        {
            if (!ExecutionContext.InProgress) return base.CanShutdown(out description);
            
            description = $"Command {ExecutionContext.Command.Id} is in progress";
            return false;
        }

        private void ProcessingCommandBehavior()
        {
            var producedEventsMetadata = ExecutionContext.CommandMetadata.CreateChild(Id, _domainEventProcessEntry);
            
            //just for catching Failures on events persist
            Command<IReadOnlyCollection<DomainEvent>>(domainEvents =>
                                   {

                                       Monitor.Increment(nameof(PersistEventPack));
                                       if (!domainEvents.Any())
                                       {
                                           Log.Warning("Trying to persist events but no events is presented. {@context}", ExecutionContext);
                                           return;
                                       }

                                       //dirty hack, but we know nobody will modify domain events before us 
                                       foreach (var evt in domainEvents)
                                           evt.ProcessId = ExecutionContext.Command.ProcessId;
                                       
                                       int messagesToPersistCount = domainEvents.Count;
                                       
                                       PersistAll(domainEvents,
                                                  persistedEvent =>
                                                  {
                                                      NotifyPersistenceWatchers(persistedEvent);
                                                      Project(persistedEvent, producedEventsMetadata);
                                                      SaveSnapshot(ExecutionContext.ProducedState, persistedEvent);

                                                      if (--messagesToPersistCount != 0) return;
                                                      
                                                      CompleteExecution();
                                                  });
                                   });

          
            Command<AllHandlersCompleted>(c =>
                                          {
                                              ExecutionContext.MessagesToProject--;
                                              if (ExecutionContext.Projecting) return;
                                              
                                              ExecutionContext.CommandSender.Tell(AggregateActor.CommandProjected.Instance);
                                              WaitForNewCommand();

                                          });
            //aggregate raised an error during command execution
            Command<Status.Failure>(f =>
                                    {
                                        ExecutionContext.Exception = f.Cause.UnwrapSingle();
                                        ExecutionContext.Validator.Tell(CommandStateActor.CommandFailed.Instance);
                                        
                                        PublishError(ExecutionContext.Exception);

                                        Behavior.Become(() =>
                                                        {
                                                            Command<AllHandlersCompleted>(c =>
                                                                throw new CommandExecutionFailedException(ExecutionContext.Command, ExecutionContext.Exception));
                                                            CommandAny(StashMessage);
                                                        },"Waiting for command exception projection");
                                    });

            DefaultBehavior();

            CommandAny(StashMessage);
        }

        private void CompleteExecution()
        {
            Log.Info("Command executed. {@context}", ExecutionContext.CommandMetadata);
            
            State = ExecutionContext.ProducedState as TAggregate;
            if(State == null)
                throw new InvalidOperationException("Aggregate state was null after command execution");
            
            State.ClearUncommitedEvents();
            
            var completedMetadata = ExecutionContext.CommandMetadata
                                                    .CreateChild(ExecutionContext.Command.Id, _commandCompletedProcessEntry);

            _publisher.Publish(AggregateActor.CommandExecuted.Instance, completedMetadata);

            ExecutionContext.CommandSender.Tell(AggregateActor.CommandExecuted.Instance);
            ExecutionContext.Validator.Tell(CommandStateActor.CommandSucceed.Instance);
            
            //waiting to some events been projecting
            if(ExecutionContext.Projecting)
                return;
                
            WaitForNewCommand();
        }

        private void WaitForNewCommand()
        {
            ExecutionContext.Clear();
            Behavior.Become(AwaitingCommandBehavior, nameof(AwaitingCommandBehavior));
            Stash.Unstash();
        }

        private IFault PublishError(Exception exception)
        {
            var command = ExecutionContext.Command;

            Log.Error(exception, "An error occured while command execution. {@context}", ExecutionContext);

            var producedFaultMetadata = ExecutionContext.CommandMetadata.CreateChild(command.Id, _domainEventProcessFailEntry);
            var fault = Fault.NewGeneric(command, exception, command.ProcessId, typeof(TAggregate));
            
            Project(fault, producedFaultMetadata);
            ExecutionContext.CommandSender.Tell(fault);
            return fault;
        }

        private void Project(object evt, IMessageMetadata commandMetadata)
        {
            ExecutionContext.MessagesToProject++;
            _customHandlersActor.Tell(new MessageMetadataEnvelop(evt, commandMetadata));
            _publisher.Publish(evt,commandMetadata);
        }
    }
    
    public static class AggregateActor
    {
        //Stages of command processing notifications: 
        // 1. Executed
        // 2. Projected
        
        public class CommandExecuted
        {
            private CommandExecuted()
            {

            }
            public static CommandExecuted Instance { get; } = new CommandExecuted();
        }
        
        public class CommandProjected
        {
            private CommandProjected()
            {

            }
            public static CommandProjected Instance { get; } = new CommandProjected();
        }
    }
}