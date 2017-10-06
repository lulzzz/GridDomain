using System;
using Akka.Actor;
using GridDomain.Node.Configuration.Hocon;

namespace GridDomain.Node.Configuration {
    public static class NodeConfigurationExtensions
    {
        public static ActorSystem CreateInMemorySystem(this AkkaConfiguration conf)
        {
            return ActorSystem.Create(conf.Network.SystemName, conf.ToStandAloneInMemorySystemConfig());
        }

        public static ActorSystem CreateSystem(this AkkaConfiguration conf)
        {
            return ActorSystem.Create(conf.Network.SystemName, conf.ToStandAloneSystemConfig());
        }

        public static string ToClusterSeedNodeSystemConfig(this AkkaConfiguration conf,params INodeNetworkAddress[] otherSeeds)
        {
            var cfg = new RootConfig(new LogConfig(conf.LogLevel, conf.LogActorType, false),
                                     ClusterConfig.SeedNode(conf.Network, otherSeeds),
                                     new PersistenceConfig(new PersistenceJournalConfig(conf.Persistence ?? throw new ArgumentNullException(nameof(conf.Persistence)), new DomainEventAdaptersConfig()),
                                                           new PersistenceSnapshotConfig(conf)));
            return cfg.Build();
        }

        public static string ToStandAloneSystemConfig(this AkkaConfiguration conf, bool serializeMessagesCreators = false)
        {
            var cfg = new RootConfig(new LogConfig(conf.LogLevel, conf.LogActorType, false),
                                     new SerializersConfig(serializeMessagesCreators, serializeMessagesCreators),
                                     new ActorProviderConfig(),
                                     new TransportConfig(conf.Network),
                                     new PersistenceConfig(new PersistenceJournalConfig(conf.Persistence ?? throw new ArgumentNullException(nameof(conf.Persistence)), 
                                                             new DomainEventAdaptersConfig()),
                                                           new PersistenceSnapshotConfig(conf)));
            return cfg.Build();
        }

        public static string ToStandAloneInMemorySystemConfig(this AkkaConfiguration conf,bool serializeMessagesCreators = false)
        {
            var cfg = new RootConfig(new LogConfig(conf.LogLevel, conf.LogActorType, false),
                                     new SerializersConfig(serializeMessagesCreators, serializeMessagesCreators),
                                     new ActorProviderConfig(),
                                     new TransportConfig(conf.Network),
                                     new PersistenceConfig(new InMemoryJournalConfig(new DomainEventAdaptersConfig()),
                                                           new LocalFilesystemSnapshotConfig()));

            return cfg.Build();
        }

        public static string ToClusterNonSeedNodeSystemConfig(this AkkaConfiguration conf, params INodeNetworkAddress[] seeds)
        {
            var cfg = new RootConfig(new LogConfig(conf.LogLevel, conf.LogActorType, false),
                                     ClusterConfig.NonSeedNode(conf.Network, seeds),
                                     new PersistenceConfig(new PersistenceJournalConfig(conf.Persistence ?? throw new ArgumentNullException(nameof(conf.Persistence)), new DomainEventAdaptersConfig()),
                                                           new PersistenceSnapshotConfig(conf)));
            return cfg.Build();
        }
    }
}