using GridDomain.Node;
using Xunit.Abstractions;

namespace GridDomain.Tests.Stress.NodeCommandExecution {
    public class CreationAndChangeWriteOnlyInMem : ScenarionPerfTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        public CreationAndChangeWriteOnlyInMem(ITestOutputHelper output) : base(output)
        {
            _testOutputHelper = output;
        }
        protected override INodeScenario Scenario { get; } = new Stress.BalloonsCreationAndChangeScenario(100, 10);

        protected override IGridDomainNode CreateNode()
        {
            return new BalloonWriteOnlyFixture(_testOutputHelper).CreateNode().Result;
        }
        
    }
}