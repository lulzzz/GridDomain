using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.Node;
using GridDomain.Node.Actors.Aggregates.Messages;
using GridDomain.Node.Actors.PersistentHub;
using GridDomain.Tests.Common;
using GridDomain.Tests.Unit;
using GridDomain.Tests.Unit.BalloonDomain;
using GridDomain.Tests.Unit.BalloonDomain.Commands;
using GridDomain.Tests.Unit.BalloonDomain.Events;
using NBench;
using Pro.NBench.xUnit.XunitExtensions;
using Serilog.Events;
using Xunit.Abstractions;

namespace GridDomain.Tests.Stress.NodeCommandExecution
{
    public class AggregateActorPerfFixtureInMem
    {
        private const string TotalCommandsExecutedCounter = "TotalCommandsExecutedCounter";
        private Counter _counter;
        private readonly ITestOutputHelper _testOutputHelper;
        private NodeTestFixture _fixture;
        private IActorRef _aggregateActor;

        public AggregateActorPerfFixtureInMem(ITestOutputHelper output)
        {
            _testOutputHelper = output;
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new XunitTraceListener(output));

            _fixture = new BalloonFixture
                       {
                           Output = _testOutputHelper,
                           AkkaConfig = new StressTestAkkaConfiguration(LogLevel.ErrorLevel),
                           LogLevel = LogEventLevel.Error
                       };

            var node = _fixture.CreateNode().Result;
            _warmUpResult = node.WarmUpAggregate<Balloon>(Guid.NewGuid()).Result;
            _aggregateActor = _warmUpResult.Info.Ref;
            _scenario = new BalloonsCreationAndChangeDirectExecutionSchenario(_aggregateActor, 1, 1);
        }

        [PerfSetup]
        public void Setup(BenchmarkContext context)
        {
            _counter = context.GetCounter(TotalCommandsExecutedCounter);
        }

        class BalloonsCreationAndChangeDirectExecutionSchenario
        {
            private readonly Random _random = new Random();
            private readonly IActorRef _aggregateActorRef;
            private ICollection<ICommand> Commands { get; }

            public BalloonsCreationAndChangeDirectExecutionSchenario(IActorRef aggregateActorRef,
                                                                     int aggregateScenariosCount,
                                                                     int aggregateChangeAmount)
            {
                _aggregateActorRef = aggregateActorRef;
                Commands = Enumerable.Range(0, aggregateScenariosCount)
                                     .SelectMany(c => CreateAggregatePlan(aggregateChangeAmount))
                                     .ToArray();
            }

            private IEnumerable<ICommand> CreateAggregatePlan(int changeAmount)
            {
                var balloonId = Guid.NewGuid();
                yield return new InflateNewBallonCommand(_random.Next(), balloonId);

                for (var num = 0; num < changeAmount; num++)
                    yield return new WriteTitleCommand(_random.Next(), balloonId);
            }

            public Task Execute(Action singlePlanExecutedCallback)
            {
                return Task.WhenAll(Commands.Select(c => _aggregateActorRef.Ask<CommandExecuted>(new MessageMetadataEnvelop<ICommand>(c, MessageMetadata.Empty))
                                                                           .ContinueWith(t => singlePlanExecutedCallback())));
            }
        }

        private BalloonsCreationAndChangeDirectExecutionSchenario _scenario;
        private WarmUpResult _warmUpResult;

        [NBenchFact]
        [PerfBenchmark(Description = "Measuring command executions by aggregate actor itself",
            NumberOfIterations = 3,
            RunMode = RunMode.Iterations,
            TestMode = TestMode.Test)]
        [CounterThroughputAssertion(TotalCommandsExecutedCounter, MustBe.GreaterThan, 1)]
        [MemoryMeasurement(MemoryMetric.TotalBytesAllocated)]
        //MAX: 500
        public void MeasureCommandExecutionWithoutProjectionsInMemory()
        {
            _scenario.Execute(() => _counter.Increment())
                     .Wait();
        }

        [PerfCleanup]
        public void Cleanup()
        {
          //  _fixture.Dispose();
        }
    }
}