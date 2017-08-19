using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Util.Internal;
using GridDomain.CQRS;
using GridDomain.Node;
using GridDomain.Node.Actors;
using GridDomain.Node.Configuration.Akka;
using GridDomain.Node.Configuration.Composition;
using GridDomain.Scheduling.Quartz;
using GridDomain.Tests.Acceptance.Snapshots;
using GridDomain.Tests.Unit;
using GridDomain.Tools.Repositories.RawDataRepositories;
using Pro.NBench.xUnit.XunitExtensions;
using NBench;
using Serilog.Events;
using Xunit.Abstractions;

namespace GridGomain.Tests.Stress
{
    public class CommandExecutionPerfFixtureInMem
    {
        private const string TotalCommandsExecutedCounter = "TotalCommandsExecutedCounter";
        private Counter _counter;
        private readonly ITestOutputHelper _testOutputHelper;
        private NodeTestFixture _fixture;

        public CommandExecutionPerfFixtureInMem(ITestOutputHelper output)
        {
            _testOutputHelper = output;
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new XunitTraceListener(output));

           
        }

        [PerfSetup]
        public void Setup(BenchmarkContext context)
        {
            _fixture = new BalloonFixture
                      {
                          Output = _testOutputHelper,
                          AkkaConfig = new StressTestAkkaConfiguration(LogLevel.ErrorLevel),
                          LogLevel = LogEventLevel.Error
                      }.UseSqlPersistence();

            _fixture.CreateNode().Wait();
            _counter = context.GetCounter(TotalCommandsExecutedCounter);
        }

        private INodeScenario Scenario { get; } = new BalloonsCreationAndChangeScenario(101, 11);

        [NBenchFact]
        [PerfBenchmark(Description = "Measuring command executions without projections in memory",
                       NumberOfIterations = 5, RunMode = RunMode.Iterations,
                       RunTimeMilliseconds = 1000, TestMode = TestMode.Test)]
        [CounterThroughputAssertion(TotalCommandsExecutedCounter, MustBe.GreaterThan, 100)]
        [MemoryMeasurement(MemoryMetric.TotalBytesAllocated)]
        //MAX: 500
        public void MeasureCommandExecutionWithoutProjectionsInMemory()
        {
            Scenario.Execute(_fixture.Node, p => _counter.Increment());
            _counter.Increment();
        }

        [PerfCleanup]
        public void Cleanup()
        {
            _fixture.Dispose();
        }
    }
}