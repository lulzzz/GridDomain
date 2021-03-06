using System;
using System.Threading;
using System.Threading.Tasks;
using GridDomain.CQRS;
using GridDomain.Node.AkkaMessaging.Waiting;
using GridDomain.Tests.Unit.BalloonDomain;
using GridDomain.Tests.Unit.BalloonDomain.Commands;
using GridDomain.Tests.Unit.BalloonDomain.ProjectionBuilders;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Unit.CommandsExecution
{
    public class Execute_command_until_projection_build_notification_fetching_results : BalloonDomainCommandExecutionTests
    {
        public Execute_command_until_projection_build_notification_fetching_results(ITestOutputHelper output) : base(output) {}

        [Fact]
        public async Task Given_command_executes_with_waiter_When_fetching_results()
        {
            var syncCommand = new PlanTitleWriteCommand(1000, Guid.NewGuid());

            var results = await Node.Prepare(syncCommand)
                                    .Expect<BalloonTitleChangedNotification>()
                                    .Execute();

            var changedEvent = results.Received;
            var aggregate = await this.LoadAggregateByActor<Balloon>(syncCommand.AggregateId);

            //Results_contains_received_messages()
            Assert.NotEmpty(results.All);
            //Results_contains_requested_message()
            Assert.NotNull(changedEvent);
            //Emmited_event_has_correct_id()
            Assert.Equal(syncCommand.AggregateId, changedEvent?.BallonId);
            //Aggregate_has_correct_state_from_command()
            Assert.Equal(syncCommand.Parameter.ToString(), aggregate.Title);
        }
    }
}