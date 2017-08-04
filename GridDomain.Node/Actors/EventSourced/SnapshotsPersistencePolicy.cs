using System;
using Akka.Persistence;
using GridDomain.Common;
using GridDomain.Configuration;
using Serilog;
using SnapshotSelectionCriteria = GridDomain.Configuration.SnapshotSelectionCriteria;

namespace GridDomain.Node.Actors.EventSourced
{
    public class SnapshotsPersistencePolicy : ISnapshotsPersistencePolicy
    {
        private readonly int _eventsToKeep;
        private readonly TimeSpan? _maxSaveFrequency;
        private readonly int _saveOnEach;
        private long _lastSavedSnapshot;
        private long _lastSequenceNumber;
        private DateTime _savedAt;

        public SnapshotsPersistencePolicy(int saveOnEach = 1,
                                          int eventsToKeep = 10,
                                          TimeSpan? maxSaveFrequency = null,
                                          DateTime? savedAt = null)
        {
            _maxSaveFrequency = maxSaveFrequency ?? TimeSpan.MinValue;
            _saveOnEach = saveOnEach;
            _eventsToKeep = eventsToKeep;
            _savedAt = savedAt ?? DateTime.MinValue;
        }

        public ILogger Log { get; set; }

        public bool ShouldSave(long snapshotSequenceNr, DateTime? now = null)
        {
            var saveIsInTime = (now ?? BusinessDateTime.UtcNow) - _savedAt >= _maxSaveFrequency;
            _lastSequenceNumber = Math.Max(snapshotSequenceNr, _lastSequenceNumber);

            if (!saveIsInTime)
            {
                Log?.Debug("will not save snapshots due to time limitations");
                return false;
            }
            if (_lastSequenceNumber % _saveOnEach != 0)
            {
                Log?.Debug("will not save snapshots due to save on event count condition");
                return false;
            }

            Log?.Debug("Saving snapshot {num}", snapshotSequenceNr);

            return true;
        }

        public bool ShouldDelete(out SnapshotSelectionCriteria snapshotsToDeleteCriteria)
        {
            var maxSnapshotNumToDelete = Math.Max(_lastSavedSnapshot - _eventsToKeep, 0);
            snapshotsToDeleteCriteria = new SnapshotSelectionCriteria(){MaxSequenceNr = maxSnapshotNumToDelete };
            return true;
        }

        public void MarkSnapshotApplied(long sequenceNr)
        {
            _lastSequenceNumber = sequenceNr;
            _lastSavedSnapshot = sequenceNr;
        }

        public void MarkSnapshotSaved(long snapshotsSequenceNumber, DateTime? saveTime = null)
        {
            Log?.Debug("Snapshot {num} saved", snapshotsSequenceNumber);
            _savedAt = saveTime ?? BusinessDateTime.UtcNow;
            _lastSavedSnapshot = snapshotsSequenceNumber;
        }
    }
}