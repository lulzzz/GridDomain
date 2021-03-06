using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GridDomain.CQRS;
using GridDomain.Tests.Acceptance.EventsUpgrade;
using GridDomain.Tests.Common;
using GridDomain.Tests.Unit;
using GridDomain.Tests.Unit.BalloonDomain;
using GridDomain.Tests.Unit.BalloonDomain.Commands;
using GridDomain.Tests.Unit.BalloonDomain.Events;
using GridDomain.Tests.Unit.ProcessManagers;
using GridDomain.Tools.Repositories.AggregateRepositories;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Acceptance.Snapshots
{
    public class Aggregate_Should_delete_snapshots_according_to_policy_on_shutdown : NodeTestKit
    {
        public Aggregate_Should_delete_snapshots_according_to_policy_on_shutdown(ITestOutputHelper output)
            : base(
                   new BalloonFixture(output).UseSqlPersistence()
                                             .InitFastRecycle()
                                             .EnableSnapshots(2)) { }

        private readonly int[] _parameters = new int[5];

        private async Task ChangeSeveralTimes(int changeNumber, string aggregateId)
        {
            for (var cmdNum = 0; cmdNum < changeNumber; cmdNum++)
            {
                _parameters[cmdNum] = cmdNum;
                await Node.Execute(new WriteTitleCommand(cmdNum, aggregateId));
            }
        }

        [Fact]
        public async Task Given_save_on_each_message_policy_and_keep_2_snapshots()
        {
            var aggregateId = Guid.NewGuid().ToString();

            await Node.Execute(new InflateNewBallonCommand(1, aggregateId));

            await ChangeSeveralTimes(5, aggregateId);

            await Node.KillAggregate<Balloon>(aggregateId,TimeSpan.FromSeconds(10));                                

            //sql server still need some time to commit deleted snapshots;
            await Task.Delay(TimeSpan.FromSeconds(3));

            var snapshots = await new AggregateSnapshotRepository(AutoTestNodeDbConfiguration.Default.JournalConnectionString,
                                                                  new BalloonAggregateFactory(),
                                                                  new BalloonAggregateFactory()).Load<Balloon>(aggregateId);

            //Only_2_Snapshots_should_left()
            Assert.Equal(2, snapshots.Length);
            //Restored_aggregates_should_have_same_ids()
            Assert.True(snapshots.All(s => s.Payload.Id == aggregateId));
            //Snapshots_should_have_parameters_from_last_command()
            Assert.Equal(_parameters.Skip(3)
                                    .Take(2)
                                    .Select(p => p.ToString())
                                    .ToArray(),
                         snapshots.Select(s => s.Payload.Title)
                                  .ToArray());
            //All_snapshots_should_not_have_uncommited_events()
            Assert.Empty(snapshots.SelectMany(s => s.Payload.GetEvents()));
        }
    }
}