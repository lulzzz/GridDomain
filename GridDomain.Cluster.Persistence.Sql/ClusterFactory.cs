using System.Linq;
using Akka.Actor;
using GridDomain.Node.Configuration;
using GridDomain.Node.Persistence.Sql;

namespace GridDomain.Cluster.Persistence.Sql
{
    public static class ClusterFactory
    {
        public static GridDomainCluster CreateCluster(this NodeConfiguration nodeConf, ISqlNodeDbConfiguration persistenceCfg, ISqlNodeDbConfiguration clusterPersistenceCfg, string name, int seedNodeNumber = 2, int childNodeNumber = 3)
        {
            var port = nodeConf.Address.PortNumber;
            var seedNodeConfigs = Enumerable.Range(0, seedNodeNumber)
                                            .Select(n => Copy(nodeConf, port++))
                                            .ToArray();

            var seedAdresses = seedNodeConfigs.Select(s => s.Address)
                                              .ToArray();

           var seedSystems =
               seedNodeConfigs.Select(
                                      c => ActorSystem.Create(name, c.ToClusterSeedNodeSystemConfig(persistenceCfg, clusterPersistenceCfg,seedAdresses)));
           
           var nonSeedConfiguration =
               Enumerable.Range(0, childNodeNumber)
                         .Select(n => ActorSystem.Create(name, nodeConf.ToClusterNonSeedNodeSystemConfig(persistenceCfg, clusterPersistenceCfg, seedAdresses)));
           
           
            return new GridDomainCluster {SeedNodes = seedSystems.ToArray(), NonSeedNodes = nonSeedConfiguration.ToArray()};
        }

        private static NodeConfiguration Copy(NodeConfiguration cfg, int newPort)
        {
            return new NodeConfiguration(cfg.Name, new NodeNetworkAddress(cfg.Address.Host, newPort),
                                         cfg.LogLevel);
        }
    }
}