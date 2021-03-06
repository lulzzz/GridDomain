using System.Collections.Generic;
using System.Threading.Tasks;
using Automatonymous;
using GridDomain.CQRS;
using GridDomain.ProcessManagers;
using GridDomain.ProcessManagers.DomainBind;
using GridDomain.Tests.Unit.BalloonDomain.Events;

namespace GridDomain.Tests.Unit.ProcessManagers.ProcessManagerActorTests
{
    public class AsyncLongRunningProcess : Process<TestState>
    {
        public AsyncLongRunningProcess()
        {
            InstanceState(s => s.CurrentStateName);

            During(Initial,
                   When(Start).ThenAsync(async (state, msg) =>{
                                             state.ProcessingId = msg.SourceId;
                                             await Task.Delay(100);
                                         }).TransitionTo(Initial),
                   When(Progress).Then((state,msg) =>
                                       {
                                           state.ProcessingId = msg.SourceId;
                                       }).TransitionTo(Final));
        }

        public static IProcessDescriptor Descriptor
        {
            get
            {
                var descriptor = ProcessDescriptor.CreateDescriptor<AsyncLongRunningProcess, TestState>();
                descriptor.AddAcceptedMessage<BalloonTitleChanged>();
                return descriptor;
            }
        }

        public Event<BalloonCreated> Start { get; private set; }
        public Event<BalloonTitleChanged> Progress { get; private set; }
        public override Task<IReadOnlyCollection<ICommand>> Transit(TestState state, object message)
        {
            switch (message)
            {
                case BalloonCreated m: return TransitMessage(Start, m, state);
                case BalloonTitleChanged m: return TransitMessage(Progress, m, state);
            }
            throw new UnbindedMessageReceivedException(message);
        }
    }
}