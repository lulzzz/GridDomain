using System;
using System.Threading.Tasks;
using Automatonymous;
using GridDomain.EventSourcing.Sagas.InstanceSagas;
using GridDomain.Tests.Framework;
using GridDomain.Tests.XUnit;
using GridDomain.Tests.XUnit.Sagas;
using GridDomain.Tests.XUnit.Sagas.SoftwareProgrammingDomain;
using GridDomain.Tools.Repositories.AggregateRepositories;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Acceptance.XUnit.Snapshots
{
    public class Instance_saga_Should_recover_from_snapshot : NodeTestKit
    {
        public Instance_saga_Should_recover_from_snapshot(ITestOutputHelper helper)
            : base(helper, new SoftwareProgrammingSagaFixture {InMemory = false}) {}

        [Fact]
        public async Task Test()
        {
            var saga  = new SoftwareProgrammingProcess();
            var state = new SoftwareProgrammingState(Guid.NewGuid(), saga.Coding.Name, Guid.NewGuid(), Guid.NewGuid());

            var sagaState = new SagaStateAggregate<SoftwareProgrammingState>(state);
            Event @event = saga.CoffeReady;
            sagaState.ReceiveMessage(state, new object());
            sagaState.ClearEvents();

            var repo = new AggregateSnapshotRepository(AkkaConfig.Persistence.JournalConnectionString,
                                                       Node.AggregateFromSnapshotsFactory);
            await repo.Add(sagaState);

            var restoredState = await this.LoadSaga<SoftwareProgrammingState>(sagaState.Id);
            //CoffeMachineId_should_be_equal()
            Assert.Equal(sagaState.State.CoffeeMachineId,  restoredState.CoffeeMachineId);
            // State_should_be_equal()
            Assert.Equal(sagaState.State.CurrentStateName, restoredState.CurrentStateName);
        }
    }
}