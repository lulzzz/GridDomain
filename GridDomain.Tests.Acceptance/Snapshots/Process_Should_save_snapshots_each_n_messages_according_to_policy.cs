using System;
using System.Linq;
using System.Threading.Tasks;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using GridDomain.Node.AkkaMessaging.Waiting;
using GridDomain.ProcessManagers.State;
using GridDomain.Tests.Common;
using GridDomain.Tests.Unit;
using GridDomain.Tests.Unit.ProcessManagers;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain.Commands;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain.Events;
using GridDomain.Tools.Repositories.AggregateRepositories;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Acceptance.Snapshots
{
    public class Process_Should_save_snapshots_each_n_messages_according_to_policy : NodeTestKit
    {
        public Process_Should_save_snapshots_each_n_messages_according_to_policy(ITestOutputHelper output)
            : base(new SoftwareProgrammingProcessManagerFixture(output)
                       .SetLogLevel(LogEventLevel.Debug)
                       .UseSqlPersistence()
                       .InitSnapshots(5, TimeSpan.FromMilliseconds(1), 2)
                       .IgnorePipeCommands()) { }

        [Fact]
        public async Task Given_default_policy()
        {
            var startEvent = new GotTiredEvent(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            var res = await Node.NewDebugWaiter()
                                .Expect<ProcessManagerCreated<SoftwareProgrammingState>>()
                                .Create()
                                .SendToProcessManagers(startEvent);

            var processId = res.Message<ProcessManagerCreated<SoftwareProgrammingState>>()
                               .SourceId;

            var continueEvent = new CoffeMakeFailedEvent(processId, startEvent.PersonId, BusinessDateTime.UtcNow, processId);

            await Node.NewDebugWaiter()
                      .Expect<ProcessReceivedMessage<SoftwareProgrammingState>>()
                      .Create()
                      .SendToProcessManagers(continueEvent);

            var continueEventB =
                new Fault<GoSleepCommand>(new GoSleepCommand(startEvent.PersonId, startEvent.LovelySofaId),
                                          new Exception(),
                                          typeof(object),
                                          processId,
                                          BusinessDateTime.Now);

            await Node.NewDebugWaiter()
                      .Expect<ProcessReceivedMessage<SoftwareProgrammingState>>()
                      .Create()
                      .SendToProcessManagers(continueEventB);

            await Node.KillProcessManager<SoftwareProgrammingProcess, SoftwareProgrammingState>(continueEvent.ProcessId);

            Version<ProcessStateAggregate<SoftwareProgrammingState>>[] snapshots = null;


            AwaitAssert(() =>
                        {
                            snapshots = new AggregateSnapshotRepository(AutoTestNodeDbConfiguration.Default.JournalConnectionString,
                                                                        AggregateFactory.Default,
                                                                        AggregateFactory.Default)
                                .Load<ProcessStateAggregate<SoftwareProgrammingState>>(processId)
                                .Result;

                            //saving on each message, maximum on each command
                            //Snapshots_should_be_saved_two_times
                            //4 events in total, two saves of snapshots due to policy saves on each two events
                            //1 event and 3
                            Assert.Equal(2, snapshots.Length);


                            //First_snapshot_should_have_state_from_first_event
                            Assert.Equal(nameof(SoftwareProgrammingProcess.Coding),
                                         snapshots.First()
                                                  .Payload.State.CurrentStateName);
                            //Last_snapshot_should_have_parameters_from_last_command()
                            Assert.Equal(nameof(SoftwareProgrammingProcess.Sleeping),
                                         snapshots.Last()
                                                  .Payload.State.CurrentStateName);
                            //Restored_process_state_should_have_correct_ids
                            Assert.True(snapshots.All(s => s.Payload.Id == processId));
                            //All_snapshots_should_not_have_uncommited_events()
                            Assert.Empty(snapshots.SelectMany(s => s.Payload.GetEvents()));
                        },
                        TimeSpan.FromSeconds(10),
                        TimeSpan.FromSeconds(1));
        }
    }
}