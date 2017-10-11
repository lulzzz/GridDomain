using System;
using Akka;
using Akka.Actor;
using GridDomain.Common;
using GridDomain.Configuration;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using GridDomain.Node.Actors.CommandPipe.Messages;
using GridDomain.Node.Actors.PersistentHub;
using GridDomain.Node.Actors.ProcessManagers.Messages;
using GridDomain.Node.AkkaMessaging;
using GridDomain.ProcessManagers;

namespace GridDomain.Node.Actors.ProcessManagers
{
    public class ProcessHubActor<TState> : PersistentHubActor where TState : class, IProcessState
    {
        public ProcessHubActor(IPersistentChildsRecycleConfiguration recycleConf, string processName): base(recycleConf, processName)
        {
            var redirectEntry = new ProcessEntry(Self.Path.Name, "Forwarding to new child", "New process was created");

            Receive<ProcessRedirect>(redirect =>
                                       {
                                           redirect.MessageToRedirect.Metadata.History.Add(redirectEntry);
                                           var name = GetChildActorName(redirect.ProcessId);
                                           SendToChild(redirect, redirect.ProcessId, name, Sender);
                                       });
        }

        protected override string GetChildActorName(Guid childId)
        {
            return AggregateActorName.New<TState>(childId).ToString();
        }

        protected override Guid GetChildActorId(IMessageMetadataEnvelop env)
        {
            var childActorId = Guid.Empty;

            if (env.Message is ProcessRedirect process)
                return process.ProcessId;

            env.Message.Match()
               .With<IFault>(m => childActorId = m.ProcessId)
               .With<IHaveProcessId>(m => childActorId = m.ProcessId);

            return childActorId;
        }

        protected override Type ChildActorType { get; } = typeof(ProcessActor<TState>);

        protected override void SendMessageToChild(ChildInfo knownChild, object message, IActorRef sender)
        {
            var msgSender = Sender;
            var self = Self;
            knownChild.Ref
                      .Ask<IProcessCompleted>(message)
                      .ContinueWith(t =>
                                    {
                                        t.Result.Match()
                                                .With<ProcessRedirect>(r => self.Tell(r, msgSender))
                                                .Default(r => msgSender.Tell(r));
                                    });
        }
    }
}