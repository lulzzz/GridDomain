using System;
using Akka.Persistence.SqlServer.Journal;
using GridDomain.Node.Configuration.Hocon;
using GridDomain.Node.Persistence.Sql;

namespace GridDomain.Cluster.Persistence.Sql {
    class ClusterSqlSnapshotsConfig : IHoconConfig
    {
        private readonly ISqlNodeDbConfiguration _dbConfiguration;
        private readonly Type _journalType;

        public ClusterSqlSnapshotsConfig(ISqlNodeDbConfiguration cfg, Type journalType = null)
        {
            _journalType = journalType;
            _dbConfiguration = cfg;
            _journalType = journalType ?? typeof(BatchingSqlServerJournal);
        }

        public string Build()
        {
            return @"sharding {
                                    class = """+ _journalType.AssemblyQualifiedName + @"""
                                    plugin-dispatcher = ""akka.actor.default-dispatcher""
                                    connection-string = """ + _dbConfiguration.SnapshotConnectionString + @"""
                                    connection-timeout = " + _dbConfiguration.SnapshotsConnectionTimeoutSeconds + @"s
                                    schema-name = "+ _dbConfiguration.SchemaName + @"
                                    table-name = """ + _dbConfiguration.SnapshotTableName + @"""
                                    auto-initialize = on
                                }
                            }";
        }
    }
}