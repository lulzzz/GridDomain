using Akka.Cluster.Sharding;

namespace GridDomain.Cluster {
    public sealed class MessageExtractor : HashCodeMessageExtractor
    {
        public MessageExtractor(int maxNumberOfShards) : base(maxNumberOfShards) { }
        public override string EntityId(object message) => (message as AggregateShardEnvelope)?.EntityId;
        public override object EntityMessage(object message) => (message as AggregateShardEnvelope)?.Message;
    }
}