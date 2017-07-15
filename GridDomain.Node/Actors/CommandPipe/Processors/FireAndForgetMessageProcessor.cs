using System.Threading.Tasks;
using Akka.Actor;

namespace GridDomain.Node.Actors.CommandPipe.Processors {


    public class FireAndForgetMessageProcessor<T> : MessageProcessor<T>
    {
        public FireAndForgetMessageProcessor(IActorRef processor) : base(processor)
        {
        }

        protected override Task GetWorkInProgressTask(Task workInProgress, Task<T> process)
        {
            return process;
        }

    }

    public class FireAndForgetMessageProcessor : IMessageProcessor
    {

        public FireAndForgetMessageProcessor(IActorRef processor)
        {
            ActorRef = processor;
        }

        public Task Process(object message, ref Task workInProgress)
        {
            ActorRef.Tell(message);
            return workInProgress;
        }

        public IActorRef ActorRef { get; }
    }
}