using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using GridDomain.Node.Configuration;

namespace GridDomain.Node
{
    public class ActorSystemFactory
    {
        public static AkkaCluster CreateCluster(AkkaConfiguration akkaConf, int seedNodeNumber = 2,
            int childNodeNumber = 3)
        {
            var port = akkaConf.Network.PortNumber;
            var name = akkaConf.Network.SystemName;
            var seedNodeConfigs = Enumerable.Range(0, seedNodeNumber)
                                            .Select(n => akkaConf.Copy(port++))
                                            .ToArray();

            var seedAdresses = seedNodeConfigs.Select(s => s.Network).ToArray();

            var seedSystemsConfiguration = seedNodeConfigs.Select(c => c.ToClusterSeedNodeSystemConfig(seedAdresses));

            var nonSeedConfiguration = Enumerable.Range(0, childNodeNumber)
                                                 .Select(n => akkaConf.ToClusterNonSeedNodeSystemConfig(seedAdresses));
   

            return new AkkaCluster {SeedNodes = CreateSystems(seedSystemsConfiguration, name),
                                    NonSeedNodes = CreateSystems(nonSeedConfiguration,name)};
        }

        private static ActorSystem[] CreateSystems(IEnumerable<string> conf, string name)
        {
            return conf.Select(c => ActorSystem.Create(name, c)).ToArray();
        }

        public static ActorSystem CreateActorSystem(AkkaConfiguration akkaConf)
        {
            var standAloneSystemConfig = akkaConf.ToStandAloneSystemConfig();
            var actorSystem = ActorSystem.Create(akkaConf.Network.SystemName, standAloneSystemConfig);
            return actorSystem;
        }
    }
}