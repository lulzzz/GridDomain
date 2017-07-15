using System.Threading.Tasks;
using Akka.Actor;

namespace GridDomain.Node.Actors.CommandPipe.Processors {
    public class ParrallelMessageProcessor<T> : MessageProcessor<T>
    {
        public ParrallelMessageProcessor(IActorRef processor) : base(processor)
        {
        }

        protected override Task GetWorkInProgressTask(Task workInProgress, Task<T> inProgress)
        {
            return Task.WhenAll(workInProgress, inProgress);
        }
    }
}