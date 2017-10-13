using System;
using GridDomain.Node.Configuration.Hocon;

namespace GridDomain.Cluster
{

    class ClusterConfig : IHoconConfig
    {
        public ClusterConfig()
        {
            
        }

        public string Build()
        {
            return @"cluster.sharding {
                            journal-plugin-id = ""akka.persistence.journal.sharding""
                            snapshot-plugin-id = ""akka.persistence.snapshot-store.sharding""
                            }";
        }
    }
}
