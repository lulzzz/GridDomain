using GridDomain.Node.Configuration;

namespace GridDomain.Cluster {
    public static class ActorSystemBuilderExtensions
    {
        public static ActorSystemBuilder Cluster(this ActorSystemBuilder builder)
        {
            builder.Add(new ClusterConfig());
            return builder;
        }
    }
}