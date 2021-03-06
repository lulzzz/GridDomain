using System;
using System.Linq;
using System.Threading.Tasks;
using GridDomain.Common;
using GridDomain.Node.AkkaMessaging.Waiting;
using GridDomain.ProcessManagers.State;
using GridDomain.Tests.Common;
using GridDomain.Tests.Unit;
using GridDomain.Tests.Unit.ProcessManagers;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain.Events;
using GridDomain.Tools.Repositories.AggregateRepositories;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Acceptance.Snapshots
{
    public class Process_actor_Should_delete_snapshots_according_to_policy_on_shutdown : NodeTestKit
    {
        public Process_actor_Should_delete_snapshots_according_to_policy_on_shutdown(ITestOutputHelper output)
            : base(new SoftwareProgrammingProcessManagerFixture(output).UseSqlPersistence()
                                                                       .InitSnapshots(2)
                                                                       .IgnorePipeCommands()) { }

        [Fact]
        public async Task Given_save_on_each_message_policy_and_keep_2_snapshots()
        {
            var startEvent = new GotTiredEvent(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            var res = await Node.NewDebugWaiter()
                                .Expect<ProcessManagerCreated<SoftwareProgrammingState>>()
                                .Create()
                                .SendToProcessManagers(startEvent);

            var processId = res.Message<ProcessManagerCreated<SoftwareProgrammingState>>()
                               .SourceId;

            var continueEventA = new CoffeMakeFailedEvent(Guid.NewGuid().ToString(),
                                                          startEvent.PersonId,
                                                          BusinessDateTime.UtcNow,
                                                          processId);

            await Node.SendToProcessManagers(continueEventA);

            await Node.KillProcessManager<SoftwareProgrammingProcess, SoftwareProgrammingState>(processId);


            Version<ProcessStateAggregate<SoftwareProgrammingState>>[] snapshots=null;


            //Only_two_Snapshots_should_left()

            AwaitAssert(() =>
                        {
                            snapshots = AggregateSnapshotRepository.New(AutoTestNodeDbConfiguration.Default.JournalConnectionString)
                                                                   .Load<ProcessStateAggregate<SoftwareProgrammingState>>(processId)
                                                                   .Result;
                            Assert.Equal(2, snapshots.Length);
                            
                            // Restored_aggregates_should_have_same_ids()
                            Assert.True(snapshots.All(s => s.Payload.Id == processId));

                            // First_Snapshots_should_have_coding_state_from_first_event()
                            Assert.Equal(nameof(SoftwareProgrammingProcess.MakingCoffee),
                                         snapshots.First()
                                                  .Payload.State.CurrentStateName);

                            //Last_Snapshots_should_have_coding_state_from_last_event()
                            Assert.Equal(nameof(SoftwareProgrammingProcess.Sleeping),
                                         snapshots.Last()
                                                  .Payload.State.CurrentStateName);

                            //All_snapshots_should_not_have_uncommited_events()
                            Assert.Empty(snapshots.SelectMany(s => s.Payload.GetEvents()));
                        },
                        TimeSpan.FromSeconds(10),
                        TimeSpan.FromSeconds(1));

         
        }
    }
}