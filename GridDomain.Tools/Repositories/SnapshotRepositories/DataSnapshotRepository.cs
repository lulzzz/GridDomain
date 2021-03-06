﻿using System.Linq;
using System.Threading.Tasks;
using GridDomain.Common;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.CommonDomain;
using GridDomain.Tools.Persistence.SqlPersistence;
using GridDomain.Tools.Repositories.RawDataRepositories;

namespace GridDomain.Tools.Repositories.SnapshotRepositories
{

    public static class DataSnapshotRepository
    {
        public static DataSnapshotRepository<T> New<T>(string connectionString) where T : class, IMemento
        {
             return new DataSnapshotRepository<T>(new RawSnapshotsRepository(connectionString));
        }
    }
    
    public class DataSnapshotRepository<T>  : IRepository<T> where T: class, IMemento
    {
        private readonly IRepository<SnapshotItem> _snapItemRepository;
        private readonly DomainSerializer _domainSerializer;

        
        public DataSnapshotRepository(IRepository<SnapshotItem> snapItemRepository)
        {
            _snapItemRepository = snapItemRepository;
            _domainSerializer = new DomainSerializer();
        }

        
        public void Dispose()
        {
            
        }

        public Task Save(string id, params T[] messages)
        {
            int seqNum = 0;
            return _snapItemRepository.Save(id,
                                            messages.Select(s => new SnapshotItem()
                                                                {
                                                                    Manifest = typeof(T).FullName,
                                                                    PersistenceId = id,
                                                                    SequenceNr =  ++seqNum,
                                                                    Snapshot = _domainSerializer.ToBinary(s),
                                                                    Timestamp = BusinessDateTime.Now
                                                                })
                                                    .ToArray());
        }

        public async Task<T[]> Load(string id)
        {
            var rawData = await _snapItemRepository.Load(id);
            return rawData.Select(d => _domainSerializer.FromBinary(d.Snapshot, typeof(T)))
                          .Cast<T>()
                          .ToArray();
        }
    }
}