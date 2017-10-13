using GridDomain.Node.Persistence.Sql;

namespace GridDomain.Cluster.Persistence.Sql {
    public class DefaultShardingDbConfiguration : ISqlNodeDbConfiguration
    {
        private readonly string _writeDbConnectionString;

        public DefaultShardingDbConfiguration(string connectionString)
        {
            _writeDbConnectionString = connectionString;
        }
        //enviroment variables - for appveour tests launch
        public virtual string SnapshotConnectionString => _writeDbConnectionString;

        public virtual string JournalConnectionString => _writeDbConnectionString;

        public virtual string MetadataTableName => "ShardingMetadata";
        public virtual string JournalTableName => "ShardingJournal";
        public virtual int JornalConnectionTimeoutSeconds => 120;
        public virtual int SnapshotsConnectionTimeoutSeconds => 120;
        public virtual string SnapshotTableName => "ShardingSnapshots";
        public virtual string SchemaName { get; } = "dbo";
    }
}