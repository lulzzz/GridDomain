using System;
using System.Threading.Tasks;
using GridDomain.EventSourcing;
using GridDomain.ProcessManagers.State;
using GridDomain.Tests.Unit;
using GridDomain.Tests.Unit.ProcessManagers;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain;
using GridDomain.Tools;
using GridDomain.Tools.Repositories.AggregateRepositories;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Acceptance.Snapshots
{
    public class Instance_process_Should_recover_from_snapshot : NodeTestKit
    {
        public Instance_process_Should_recover_from_snapshot(ITestOutputHelper helper)
            : base(new SoftwareProgrammingProcessManagerFixture(helper).UseSqlPersistence()) {}

        [Fact]
        public async Task Test()
        {
            var i = Node.Id; //just start node
            var process  = new SoftwareProgrammingProcess();
            var state = new SoftwareProgrammingState(Guid.NewGuid().ToString(), process.Coding.Name, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            var processStateAggregate = new ProcessStateAggregate<SoftwareProgrammingState>(state);
            processStateAggregate.ReceiveMessage(state, null);
            processStateAggregate.CommitAll();

            var repo = new AggregateSnapshotRepository(AutoTestNodeDbConfiguration.Default.JournalConnectionString,
                                                       AggregateFactory.Default,
                                                       AggregateFactory.Default);
            await repo.Add(processStateAggregate);

            var restoredState = await this.LoadProcessByActor<SoftwareProgrammingState>(processStateAggregate.Id);
            //CoffeMachineId_should_be_equal()
            Assert.Equal(processStateAggregate.State.CoffeeMachineId,  restoredState.CoffeeMachineId);
            // State_should_be_equal()
            Assert.Equal(processStateAggregate.State.CurrentStateName, restoredState.CurrentStateName);
        }
    }
}