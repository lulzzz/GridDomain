using Automatonymous.Contexts;
using GridDomain.Node.Configuration;
using GridDomain.Node.Persistence.Sql;

namespace GridDomain.Cluster.Persistence.Sql
{

    public static class ActorSystembuilderExtensions
    {
        public static ActorSystemBuilder ClusterSqlPersistence(this ActorSystemBuilder builder, ISqlNodeDbConfiguration cfg)
        {
            builder.Add(new ClusterSqlJournalConfig(cfg));   
            builder.Add(new ClusterSqlSnapshotsConfig(cfg));
            return builder;
        }
    }
}
