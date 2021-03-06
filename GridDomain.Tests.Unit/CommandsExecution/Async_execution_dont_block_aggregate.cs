using System;
using System.Linq;
using System.Threading.Tasks;
using GridDomain.CQRS;
using GridDomain.Node.AkkaMessaging.Waiting;
using GridDomain.Tests.Unit.BalloonDomain;
using GridDomain.Tests.Unit.BalloonDomain.Commands;
using GridDomain.Tests.Unit.BalloonDomain.Events;
using GridDomain.Tests.Unit.FutureEvents.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Unit.CommandsExecution
{

    public class Async_execution_dont_block_aggregate : BalloonDomainCommandExecutionTests
    {
        public Async_execution_dont_block_aggregate(ITestOutputHelper output) : base(output) {}

        [Fact]
        public async Task When_async_method_is_called_other_commands_can_be_executed_before_async_results()
        {
            var aggregateId = Guid.NewGuid().ToString();
            var asyncCommand = new PlanTitleChangeCommand(43, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), TimeSpan.FromSeconds(1));
            var syncCommand = new WriteTitleCommand(42, aggregateId);

            var asyncCommandTask = Node.Prepare(asyncCommand)
                                       .Expect<BalloonTitleChanged>()
                                       .Execute();

            await Node.Prepare(syncCommand)
                      .Expect<BalloonTitleChanged>()
                      .Execute();

            var sampleAggregate = await this.LoadAggregateByActor<Balloon>(syncCommand.AggregateId);

            Assert.Equal(syncCommand.Parameter.ToString(), sampleAggregate.Title);

            var changedEvent = (await asyncCommandTask).Message<BalloonTitleChanged>();
            Assert.Equal(asyncCommand.Parameter.ToString(), changedEvent.Value);
        }
    }
}