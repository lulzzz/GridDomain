using GridDomain.Common;

namespace GridDomain.Cluster {
    public sealed class AggregateShardEnvelope : IMessageMetadataEnvelop
    {
        public readonly string EntityId;

        public AggregateShardEnvelope(string entityId, object payload)
        {
            EntityId = entityId;
            Message = payload;
        }

        public object Message { get; }
        public IMessageMetadata Metadata { get; }
    }
}