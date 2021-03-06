using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using GridDomain.CQRS;
using GridDomain.Node.AkkaMessaging.Waiting;
using GridDomain.Tests.Common;
using GridDomain.Tests.Unit.BalloonDomain;
using GridDomain.Tests.Unit.BalloonDomain.Commands;
using GridDomain.Tests.Unit.BalloonDomain.Events;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Unit.CommandsExecution
{
    public class When_execute_command_causing_an_aggregate_error : BalloonDomainCommandExecutionTests
    {
        public When_execute_command_causing_an_aggregate_error(ITestOutputHelper output) : base(output) {}

        [Fact]
        public async Task Given_async_aggregate_method_Then_execute_throws_exception_from_aggregate()
        {
            await (Task<BalloonException>) Node.Execute(new PlanBallonBlowCommand(Guid.NewGuid().ToString())).ShouldThrow((Predicate<BalloonException>) null);
        }

        [Fact]
        public async Task Given_async_aggregate_method_Then_execute_throws_exception_from_aggregate_with_stack_trace()
        {
            var syncCommand = new PlanTitleWriteAndBlowCommand(42,
                                                                Guid.NewGuid().ToString(),
                                                                TimeSpan.FromMilliseconds(500));

            await (Task<BalloonException>) Node.Execute(syncCommand).ShouldThrow((Predicate<BalloonException>) (ex => ex.StackTrace.Contains(typeof(Balloon).Name) || 
                                                                                  ex.Message.Contains(typeof(Balloon).Name)));
        }

        [Fact]
        public async Task Given_sync_aggregate_method_Then_execute_throws_exception_from_aggregate_with_stack_trace()
        {
            await (Task<BalloonException>) Node.Execute(new BlowBalloonCommand(Guid.NewGuid().ToString())).ShouldThrow((Predicate<BalloonException>) (ex => ex.StackTrace.Contains(typeof(Balloon).Name) ||
                                                                                                                        ex.Message.Contains(typeof(Balloon).Name)));
        }
        
        [Fact]
        public async Task Given_aggregate_When_executing_several_commands_in_same_inbox_and_part_fails_Then_other_should_be_executed()
        {
            var waiter = Node.NewWaiter()
                             .Expect<BalloonTitleChanged>()
                             .Create();

            var aggregateId = "test_balloon";
            //aggregate should fail after blow command
            //and restores after write title, no loosing write title command
            Node.Execute(new InflateNewBallonCommand(1, aggregateId)); 
            Node.Execute(new BlowBalloonCommand(aggregateId)); 
            Node.Execute(new WriteTitleCommand(2,aggregateId)); 

            var evt = await waiter;
            Assert.Equal("2",evt.Message<BalloonTitleChanged>().Value);
        }
    }
}