using System;
using System.Linq;
using System.Threading.Tasks;
using GridDomain.Common;
using GridDomain.EventSourcing.Sagas;
using GridDomain.EventSourcing.Sagas.InstanceSagas;
using GridDomain.Tests.Acceptance.XUnit.EventsUpgrade;
using GridDomain.Tests.Framework;
using GridDomain.Tests.XUnit;
using GridDomain.Tests.XUnit.Sagas;
using GridDomain.Tests.XUnit.Sagas.SoftwareProgrammingDomain;
using GridDomain.Tests.XUnit.Sagas.SoftwareProgrammingDomain.Events;
using GridDomain.Tools.Repositories.AggregateRepositories;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Acceptance.XUnit.Snapshots
{
    public class Instance_Saga_Should_delete_snapshots_according_to_policy_on_shutdown : NodeTestKit
    {
        public Instance_Saga_Should_delete_snapshots_according_to_policy_on_shutdown(ITestOutputHelper output)
            : base(
                   output,
                   new SoftwareProgrammingSagaFixture {InMemory = false}.InitSoftwareProgrammingSagaSnapshots(2)
                                                                        .IgnoreCommands()) {}

        [Fact]
        public async Task Given_save_on_each_message_policy_and_keep_2_snapshots()
        {
            var sagaId = Guid.NewGuid();
            var sagaStartEvent = new GotTiredEvent(sagaId, Guid.NewGuid(), Guid.NewGuid(), sagaId);

            await
                Node.NewDebugWaiter()
                    .Expect<SagaCreated<SoftwareProgrammingState>>()
                    .Create()
                    .SendToSagas(sagaStartEvent);

            var sagaContinueEventA = new CoffeMakeFailedEvent(sagaId,
                                                              sagaStartEvent.PersonId,
                                                              BusinessDateTime.UtcNow,
                                                              sagaId);


            await
                Node.NewDebugWaiter()
                    .Expect<SagaReceivedMessage<SoftwareProgrammingState>>()
                    .Create()
                    .SendToSagas(sagaContinueEventA);

            await Node.KillSaga<SoftwareProgrammingProcess, SoftwareProgrammingState>(sagaId);

            var snapshots =
                await
                    new AggregateSnapshotRepository(AkkaConfig.Persistence.JournalConnectionString,
                                                    Node.AggregateFromSnapshotsFactory).Load<SagaStateAggregate<SoftwareProgrammingState>>(sagaId);

            //Only_two_Snapshots_should_left()
            Assert.Equal(2, snapshots.Length);
            // Restored_aggregates_should_have_same_ids()
            Assert.True(snapshots.All(s => s.Aggregate.Id == sagaId));

            // First_Snapshots_should_have_coding_state_from_first_event()
            Assert.Equal(nameof(SoftwareProgrammingProcess.MakingCoffee), snapshots.First().Aggregate.State.CurrentStateName);

            //Last_Snapshots_should_have_coding_state_from_last_event()
            Assert.Equal(nameof(SoftwareProgrammingProcess.Sleeping), snapshots.Last().Aggregate.State.CurrentStateName);

            //All_snapshots_should_not_have_uncommited_events()
            Assert.Empty(snapshots.SelectMany(s => s.Aggregate.GetEvents()));
        }
    }
}