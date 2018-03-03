using System;

namespace GridDomain.Configuration
{
    public interface ISnapshotsPersistencePolicy
    {
        //Save operations
        bool ShouldSave(long snapshotSequenceNr, DateTime? now = null);
        void MarkSnapshotApplied(long sequenceNr);
        void MarkSnapshotSaved(long snapshotSequenceNumber, DateTime? saveTime = null);

        //Delete operations
        bool ShouldDelete(out SnapshotSelectionCriteria deleteDelegate);
    }
    
    public interface ISnapshotsPersistencePolicy2
    {
        ISnapshotsSavePolicy SavePolicy { get; }
        ISnapshotsDeletePolicy DeletePolicy { get; }
    }

    public interface ISnapshotsSavePolicy
    {
        //Save operations
        int  InProgress { get; }
        bool ShouldSave(long snapshotSequenceNr, DateTime? now = null);
        
        void Applied(long sequenceNr);
        void Saved(long snapshotSequenceNumber, DateTime? saveTime = null);
        void Saving(long snapshotSequenceNumber, DateTime? saveTime = null);
    }
    
    public interface ISnapshotsDeletePolicy
    {
        //Delete operations
        int  DeleteOperationsInProgress { get; }
        bool ShouldDelete(int lastSpanshotSaved, out SnapshotSelectionCriteria deleteDelegate);
        
        void Deleting(SnapshotSelectionCriteria deleteDelegate);
        void Deleted(SnapshotSelectionCriteria deleteDelegate);
    }

    public class SnapshotsSavePolicy : ISnapshotsSavePolicy
    {
        public TimeSpan MaxSaveFrequency { get; }
        public SnapshotsSavePolicy(int eventsToKeep, int saveOnEach, TimeSpan maxSaveFrequency)
        {
            throw new NotImplementedException();
        }

        public bool ShouldSave(long snapshotSequenceNr, DateTime? now = null)
        {
            throw new NotImplementedException();
        }

        public void MarkSnapshotApplied(long sequenceNr)
        {
            throw new NotImplementedException();
        }

        public void MarkSnapshotSaved(long snapshotSequenceNumber, DateTime? saveTime = null)
        {
            throw new NotImplementedException();
        }

        public void MarkSnapshotSaving(long snapshotSequenceNumber, DateTime? saveTime = null)
        {
            throw new NotImplementedException();
        }

        public int SaveOperationsInProgress { get; }

        public void MarkSnapshotSaveFailed()
        {
            throw new NotImplementedException();
        }
    }

    public class SnapshotsDeletePolicy : ISnapshotsDeletePolicy
    {
        public bool ShouldDelete(int lastSpanshotSaved, out SnapshotSelectionCriteria deleteDelegate)
        {
            throw new NotImplementedException();
        }

        public bool MarkSnapshotsDeleting(SnapshotSelectionCriteria deleteDelegate)
        {
            throw new NotImplementedException();
        }

        public bool MarkSnapshotsDeleted(SnapshotSelectionCriteria deleteDelegate)
        {
            throw new NotImplementedException();
        }

        public int DeleteOperationsInProgress { get; }
    }
  
}