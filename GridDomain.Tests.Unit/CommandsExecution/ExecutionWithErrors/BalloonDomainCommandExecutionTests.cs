using GridDomain.Tests.Unit.BalloonDomain;
using GridDomain.Tests.Unit.BalloonDomain.Configuration;
using Xunit.Abstractions;

namespace GridDomain.Tests.Unit.CommandsExecution
{
    public class BalloonDomainCommandExecutionTests : NodeTestKit
    {
        public BalloonDomainCommandExecutionTests(ITestOutputHelper output)
            : base(new NodeTestFixture(output,new BalloonDomainConfiguration())) {}
    }
}