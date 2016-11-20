using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Persistence;
using CommonDomain;
using CommonDomain.Persistence;
using GridDomain.CQRS.Messaging;
using GridDomain.EventSourcing;
using GridDomain.Logging;
using GridDomain.Node.AkkaMessaging;

namespace GridDomain.Node.Actors
{
    public class EventSourcedActor<T> : ReceivePersistentActor where T: IAggregate
    {
        private readonly List<IActorRef> _persistenceWaiters = new List<IActorRef>();
        protected Guid Id { get; }
        protected readonly SnapshotsSavePolicy SnapshotsPolicy;
        protected readonly ActorMonitor Monitor;
        protected readonly ISoloLogger _log = LogManager.GetLogger();
        protected readonly IPublisher Publisher;
        private readonly IConstructAggregates _aggregateConstructor;
        public override string PersistenceId { get; }
        public IAggregate State { get; protected set; }

        public EventSourcedActor(IConstructAggregates aggregateConstructor,
                                 SnapshotsSavePolicy policy,
                                 IPublisher publisher)
        {
            PersistenceId = Self.Path.Name;
            SnapshotsPolicy = policy;
            _aggregateConstructor = aggregateConstructor;
            Publisher = publisher;
            Id = AggregateActorName.Parse<T>(Self.Path.Name).Id;
            State = aggregateConstructor.Build(typeof(T), Id, null);
            Monitor = new ActorMonitor(Context, typeof(T).Name);

            Command<GracefullShutdownRequest>(req =>
            {
                Monitor.IncrementMessagesReceived();
                DeleteSnapshots(SnapshotsPolicy.SnapshotsToDelete());
                Become(Terminating);
            });

            Command<CheckHealth>(s => Sender.Tell(new HealthStatus(s.Payload)));

            Command<SaveSnapshotSuccess>(s =>
            {
                NotifyWatchers(s);
                SnapshotsPolicy.SnapshotWasSaved(s.Metadata);
            });

            Command<NotifyOnPersistenceEvents>(c =>
            {
                var waiter = c.Waiter ?? Sender;
                if (IsRecoveryFinished)
                {
                    waiter.Tell(RecoveryCompleted.Instance);
                }
                else _persistenceWaiters.Add(waiter);
            });


            Recover<DomainEvent>(e =>
            {
                State.ApplyEvent(e);
                SnapshotsPolicy.RefreshActivity(e.CreatedTime);
            });

            Recover<SnapshotOffer>(offer =>
            {
                SnapshotsPolicy.SnapshotWasApplied(offer.Metadata);
                State = _aggregateConstructor.Build(typeof(T), Id, (IMemento)offer.Snapshot);
            });

            Recover<RecoveryCompleted>(message =>
            {
                Log.Debug("Recovery for actor {Id} is completed", PersistenceId);
                NotifyWatchers(message);
            });
        }

        protected bool TrySaveSnapshot(object[] stateChange)
        {
            var shouldSave = SnapshotsPolicy.ShouldSave(stateChange);
            if (shouldSave)
                SaveSnapshot(State.GetSnapshot());
            return shouldSave;
        }

        private void NotifyWatchers(object msg)
        {
            foreach (var watcher in _persistenceWaiters)
                watcher.Tell(msg);
        }

        private void Terminating()
        {
            Command<DeleteSnapshotsSuccess>(s =>
            {
                NotifyWatchers(s);
                Context.Stop(Self);
            });
            Command<DeleteSnapshotsFailure>(s =>
            {
                NotifyWatchers(s);
                Context.Stop(Self);
            });
        }

        protected override void OnPersistFailure(Exception cause, object @event, long sequenceNr)
        {
            Log.Error("Additional persistence diagnostics on fauilure {error} {actor} {event}", cause, Self.Path.Name, @event);
            base.OnPersistFailure(cause, @event, sequenceNr);
        }

        protected override void OnPersistRejected(Exception cause, object @event, long sequenceNr)
        {
            Log.Error("Additional persistence diagnostics on rejected {error} {actor} {event}", cause, Self.Path.Name, @event);
            base.OnPersistRejected(cause, @event, sequenceNr);
        }

        protected override void PreStart()
        {
            Monitor.IncrementActorStarted();
        }

        protected override void PostStop()
        {
            Monitor.IncrementActorStopped();
        }

        protected override void PreRestart(Exception reason, object message)
        {
            Monitor.IncrementActorRestarted();
        }
    }
}