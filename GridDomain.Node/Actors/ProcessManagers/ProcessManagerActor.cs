﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.DI.Core;
using Akka.Event;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using GridDomain.Node.Actors.CommandPipe.Messages;
using GridDomain.Node.Actors.EventSourced.Messages;
using GridDomain.Node.Actors.ProcessManagers.Exceptions;
using GridDomain.Node.Actors.ProcessManagers.Messages;
using GridDomain.Node.AkkaMessaging;
using GridDomain.ProcessManagers;
using GridDomain.ProcessManagers.Creation;
using GridDomain.ProcessManagers.State;
using GridDomain.Routing;

namespace GridDomain.Node.Actors.ProcessManagers
{
    //TODO: add status info, e.g. was any errors during execution or recover
    /// <summary>
    ///     Name should be parse by AggregateActorName
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    public class ProcessManagerActor<TState> : ReceiveActor,
                                     IWithUnboundedStash where TState : class, IProcessState
    {
        private readonly ProcessEntry _exceptionOnTransit;
        private readonly ProcessEntry _producedCommand;
        private readonly IProcessManagerCreatorCatalog<TState> _сreatorCatalog;
        private readonly IPublisher _publisher;
        private readonly ILoggingAdapter _log;
        private BehaviorQueue Behavior { get; }
        private ActorMonitor Monitor { get; }
        private readonly IActorRef _stateAggregateActor;

        public IProcessManager<TState> ProcessManager { get; private set; }

        public IStash Stash { get; set; }

        private Guid Id { get; }

        public ProcessManagerActor(IProcessManagerCreatorCatalog<TState> сreatorCatalog,
                         IPublisher publisher)

        {
            Monitor = new ActorMonitor(Context, "Process" + typeof(TState).Name);
            Behavior = new BehaviorQueue(Become);

            Guid id;
            if (!AggregateActorName.TryParseId(Self.Path.Name, out id))
                throw new BadNameFormatException();
            Id = id;

            _publisher = publisher;
            _сreatorCatalog = сreatorCatalog;
            _log = Context.GetLogger();

            _exceptionOnTransit = ProcessManagerActorConstants.ExceptionOnTransit(Self.Path.Name);
            _producedCommand = ProcessManagerActorConstants.ProcessProduceCommands(Self.Path.Name);

            var stateActorProps = Context.DI()
                                         .Props(typeof(ProcessStateActor<TState>));

            _stateAggregateActor = Context.ActorOf(stateActorProps,
                                                   AggregateActorName.New<ProcessStateAggregate<TState>>(Id)
                                                                     .Name);
            _stateAggregateActor.Tell(GetProcessState.Instance);
            Behavior.Become(InitializingBehavior, nameof(InitializingBehavior));
        }

        private void StashingMessagesToProcessBehavior()
        {
            Receive<IMessageMetadataEnvelop>(m => StashMessage(m));
            Receive<ProcessRedirect>(m => StashMessage(m));
            ProxifyingCommandsBehavior();
        }

        private void InitializingBehavior()
        {
            Receive<ProcesStateMessage<TState>>(ss =>
                                       {
                                           if(ss.State != null ) //having some state already persisted
                                             ProcessManager = _сreatorCatalog.Create(ss.State);
                                           FinishInitialization();
                                       });

            StashingMessagesToProcessBehavior();
        }

        private void FinishInitialization()
        {
            Behavior.Become(AwaitingMessageBehavior, nameof(AwaitingMessageBehavior));
            Stash.UnstashAll();
        }

        private void StashMessage(object m)
        {
            _log.Warning("Process {id} stashing message {messge}", Id, m);
            Stash.Stash();
        }

        private void AwaitingMessageBehavior()
        {
            Receive<IMessageMetadataEnvelop>(env =>
                                             {
                                                 if (_сreatorCatalog.CanCreateFrom(env.Message))
                                                 {
                                                     Self.Tell(new CreateNewProcess(env), Sender);
                                                     Behavior.Become(CreatingProcessBehavior, nameof(CreatingProcessBehavior));
                                                 }
                                                 else
                                                 {
                                                     if(ProcessManager.State.Id != GetProcessId(env.Message))
                                                     {
                                                         _log.Error("Existing process {process} {processId} received message {@message} "
                                                                    + "targeting different process. Process will not proceed.", typeof(TState), Id, env);

                                                         FinishWithError(env, Sender, new ProcessIdMismatchException());
                                                         return;
                                                     }

                                                     Self.Tell(env, Sender);
                                                     Behavior.Become(TransitingProcessBehavior, nameof(TransitingProcessBehavior));
                                                 }
                                             });

            Receive<ProcessRedirect>(env =>
                                       {
                                           Self.Tell(new CreateNewProcess(env.MessageToRedirect, env.ProcessId), Sender);
                                           Behavior.Become(CreatingProcessBehavior, nameof(CreatingProcessBehavior));
                                       });

            ProxifyingCommandsBehavior();
        }

        private class CreateNewProcess
        {
            public IMessageMetadataEnvelop Message { get; }
            public Guid? EnforcedId { get; }

            public CreateNewProcess(IMessageMetadataEnvelop message, Guid? enforcedId = null)
            {
                Message = message;
                EnforcedId = enforcedId;
            }
        }

        private void ProxifyingCommandsBehavior()
        {
            Receive<GracefullShutdownRequest>(r =>
                                              {
                                                  Context.Watch(_stateAggregateActor);
                                                  Become(() => Receive<Terminated>(t => Context.Stop(Self),
                                                                                   t => t.ActorRef.Path == _stateAggregateActor.Path));
                                                  _stateAggregateActor.Tell(r);
                                              });


            ReceiveAsync<CheckHealth>(s => _stateAggregateActor.Ask<HealthStatus>(s)
                                                               .PipeTo(Sender));

            Receive<NotifyOnPersistenceEvents>(c => _stateAggregateActor.Tell(c, Sender));
        }

        private void CreatingProcessBehavior()
        {
            TState pendingState = null;
            IMessageMetadataEnvelop processingMessage = null;
            IActorRef processingMessageSender = null;
            Receive<CreateNewProcess>(c =>
                                   {
                                       processingMessage = c.Message;
                                       processingMessageSender = Sender;

                                       var processManager = _сreatorCatalog.CreateNew(processingMessage.Message, c.EnforcedId);
                                       if (Id != processManager.State.Id)
                                       {
                                           FinishProcessTransition(new ProcessRedirect(processManager.State.Id, processingMessage), Sender);
                                           return;
                                       }

                                       if(ProcessManager != null)
                                           throw new ProcessAlreadyStartedException(ProcessManager.State, processingMessage);

                                       pendingState = processManager.State;
                                       var cmd = new CreateNewStateCommand<TState>(Id, pendingState);

                                       _stateAggregateActor.Ask<CommandCompleted>(new MessageMetadataEnvelop<ICommand>(cmd, processingMessage.Metadata))
                                                           .PipeTo(Self);
                                   });

            Receive<Status.Failure>(f => FinishWithError(processingMessage, processingMessageSender, f.Cause));

            //from state aggregate actro after persist
            Receive<CommandCompleted>(c =>
                                      {
                                          ProcessManager = _сreatorCatalog.Create(pendingState);
                                          Self.Tell(processingMessage, processingMessageSender);
                                          Behavior.Become(TransitingProcessBehavior, nameof(TransitingProcessBehavior));
                                          pendingState = null;
                                          processingMessage = null;
                                          processingMessageSender = null;
                                      });
            StashingMessagesToProcessBehavior();

        }

        private void TransitingProcessBehavior()
        {
            IMessageMetadataEnvelop processingEnvelop = null;
            TState pendingState = null;
            IReadOnlyCollection<ICommand> producedCommands = new ICommand[]{};
            IActorRef processingMessageSender = null;


            Receive<IMessageMetadataEnvelop>(messageEnvelop =>
                                             {
                                                      processingEnvelop = messageEnvelop;
                                                      processingMessageSender = Sender;
                                                      if (ProcessManager == null)
                                                      {
                                                          _log.Error("Process {process} {processId} is not started but received transition message {@message}. "
                                                                       + "Process will not proceed. ", typeof(TState), Id, messageEnvelop);
                                                          Task.FromException(new ProcessNotStartedException()).PipeTo(Self);
                                                          return;
                                                      }

                                                      Task<ProcessResult<TState>> processTask = ProcessManager.Transit((dynamic)messageEnvelop.Message);
                                                      processTask.PipeTo(Self);
                                                  });

            ReceiveAsync<ProcessResult<TState>>(transitionResult =>
                                              {
                                                  pendingState = transitionResult.State;
                                                  producedCommands = transitionResult.ProducedCommands;
                                                  var cmd = new SaveStateCommand<TState>(Id,
                                                                                         pendingState,
                                                                                         ProcessManager.State.CurrentStateName,
                                                                                         processingEnvelop);

                                                  return _stateAggregateActor.Ask<CommandCompleted>(new MessageMetadataEnvelop<ICommand>(cmd, processingEnvelop.Metadata))
                                                                             .PipeTo(Self);
                                              });
            Receive<CommandCompleted>(c =>
                                      {
                                          ProcessManager = _сreatorCatalog.Create(pendingState);
                                          FinishProcessTransition(new ProcessTransited(producedCommands.ToArray(),
                                                                                 processingEnvelop.Metadata,
                                                                                 _producedCommand,
                                                                                 pendingState),
                                                               processingMessageSender);
                                          Behavior.Become(AwaitingMessageBehavior, nameof(AwaitingMessageBehavior));
                                          pendingState = null;
                                          processingMessageSender = null;
                                          processingEnvelop = null;
                                          producedCommands = null;
                                      });

            Receive<Status.Failure>(f => FinishWithError(processingEnvelop, processingMessageSender, f.Cause));

            StashingMessagesToProcessBehavior();
        }

        private void FinishWithError(IMessageMetadataEnvelop processingMessage, IActorRef messageSender, Exception erorr)
        {
            var fault = CreateFault(processingMessage.Message,
                                    erorr.UnwrapSingle());

            var faultMetadata = processingMessage.Metadata.CreateChild(fault.ProcessId, _exceptionOnTransit);

            _publisher.Publish(fault, faultMetadata);

            FinishProcessTransition(new ProcessFault(fault, processingMessage.Metadata), messageSender);
            
            Behavior.Become(AwaitingMessageBehavior, nameof(AwaitingMessageBehavior));
        }

        private void FinishProcessTransition(IProcessCompleted message, IActorRef messageSender)
        {
            messageSender.Tell(message);
            Stash.UnstashAll();
        }

        private IFault CreateFault(object message, Exception exception)
        {
            var processorType = ProcessManager?.GetType() ?? typeof(TState);
            _log.Error(exception, "Process {process} {id} raised an error on {@message}", processorType, Id, message);
            return Fault.NewGeneric(message, exception, Id, processorType);
        }

        private Guid GetProcessId(object msg)
        {
            Guid processId = Guid.Empty;
            msg.Match()
               .With<IFault>(m => processId = m.ProcessId)
               .With<IHaveProcessId>(m => processId = m.ProcessId);
            return processId;
        }
    }

}