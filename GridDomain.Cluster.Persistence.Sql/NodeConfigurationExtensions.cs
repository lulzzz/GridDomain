using Akka.Actor;
using GridDomain.Node.Configuration;
using GridDomain.Node.Persistence.Sql;

namespace GridDomain.Cluster.Persistence.Sql
{

    public static class NodeConfigurationExtensions
    {

        public static string ToClusterSeedNodeSystemConfig(this NodeConfiguration conf, ISqlNodeDbConfiguration cfg, ISqlNodeDbConfiguration clusterCfg, params INodeNetworkAddress[] otherSeeds)
        {
            return ActorSystemBuilder.New()
                                     .Log(conf.LogLevel)
                                     .ClusterSeed(conf, otherSeeds)
                                     .SqlPersistence(cfg)
                                     .Cluster()
                                     .ClusterSqlPersistence(clusterCfg)
                                     .BuildHocon();
        }

         public static string ToClusterNonSeedNodeSystemConfig(this NodeConfiguration conf, ISqlNodeDbConfiguration persistence, ISqlNodeDbConfiguration clusterCfg, params INodeNetworkAddress[] seeds)
         {
             return ActorSystemBuilder.New()
                                      .Log(conf.LogLevel)
                                      .ClusterNonSeed(conf, seeds)
                                      .SqlPersistence(persistence)
                                      .Cluster()
                                      .ClusterSqlPersistence(clusterCfg)
                                      .BuildHocon();
         }
    }
}