using System;
using Akka.Persistence.SqlServer.Journal;
using GridDomain.Node.Configuration.Hocon;
using GridDomain.Node.Persistence.Sql;

namespace GridDomain.Cluster.Persistence.Sql {
    class ClusterSqlJournalConfig : IHoconConfig
    {
        private readonly ISqlNodeDbConfiguration _dbConfiguration;
        private readonly Type _journalType;

        public ClusterSqlJournalConfig(ISqlNodeDbConfiguration cfg, Type journalType = null)
        {
            _journalType = journalType;
            _dbConfiguration = cfg;
            _journalType = journalType ?? typeof(BatchingSqlServerJournal);
        }

        public string Build()
        {
            return @"
            sharding {
                      class = """ + _journalType.AssemblyQualifiedName + @"""
                      plugin-dispatcher = ""akka.actor.default-dispatcher""
                      connection-string =  """ + _dbConfiguration.JournalConnectionString + @"""
                      connection-timeout = " + _dbConfiguration.JornalConnectionTimeoutSeconds + @"s
                      schema-name = " + _dbConfiguration.SchemaName + @"
                      table-name = """ + _dbConfiguration.JournalTableName + @"""
                      auto-initialize = on
                      timestamp-provider = ""Akka.Persistence.Sql.Common.Journal.DefaultTimestampProvider, Akka.Persistence.Sql.Common""
                      metadata-table-name = """ + _dbConfiguration.MetadataTableName + @"""
             }";
        }
    }
}