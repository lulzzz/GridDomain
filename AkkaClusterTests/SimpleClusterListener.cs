using Akka.Actor;
using Akka.Cluster;
using Akka.Event;

namespace AkkaClusterTests
{
    public class SimpleClusterListener : UntypedActor
    {
        private string _key;
        protected Cluster Cluster = Cluster.Get(Context.System);
        protected ILoggingAdapter Log = Context.GetLogger();

        public SimpleClusterListener(string key)
        {
            _key = key;
        }

        /// <summary>
        ///     Need to subscribe to cluster changes
        /// </summary>
        protected override void PreStart()
        {
            Cluster.Subscribe(Self, ClusterEvent.InitialStateAsEvents,
                new[] {typeof (ClusterEvent.IMemberEvent), typeof (ClusterEvent.UnreachableMember)});
        }

        /// <summary>
        ///     Re-subscribe on restart
        /// </summary>
        protected override void PostStop()
        {
            Cluster.Unsubscribe(Self);
        }

        protected override void OnReceive(object message)
        {
            var up = message as ClusterEvent.MemberUp;
            if (up != null)
            {
                var mem = up;
                Log.Info("Member is Up: {0}", mem.Member);
            }
            else if (message is ClusterEvent.UnreachableMember)
            {
                var unreachable = (ClusterEvent.UnreachableMember) message;
                Log.Info("Member detected as unreachable: {0}", unreachable.Member);
            }
            else if (message is ClusterEvent.MemberRemoved)
            {
                var removed = (ClusterEvent.MemberRemoved) message;
                Log.Info("Member is Removed: {0}", removed.Member);
            }
            else if (message is ClusterEvent.IMemberEvent)
            {
                //IGNORE                
            }
            else if (message is ClusterEvent.CurrentClusterState)
            {
            }
            else
            {
                Unhandled(message);
            }
        }
    }
}