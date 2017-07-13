using System.Threading.Tasks;
using GridDomain.Node;
using GridDomain.Scheduling;
using GridDomain.Scheduling.Quartz.Configuration;
using GridDomain.Tests.Unit.DependencyInjection.FutureEvents.Configuration;
using Xunit.Abstractions;

namespace GridDomain.Tests.Unit.DependencyInjection.FutureEvents
{
    internal class FutureEventsFixture : NodeTestFixture
    {
        public FutureEventsFixture(ITestOutputHelper output = null) : base(null, null, output)
        {
            Add(() => new FutureAggregateDomainConfiguration(() => System.GetExtension<SchedulingExtension>().SchedulingActor));
            this.ClearSheduledJobs();
        }

        //protected override NodeSettings CreateNodeSettings()
        //{
        //    var settings = base.CreateNodeSettings();
        //
        //
        //}

        public override Task<GridDomainNode> CreateNode()
        {
            return base.CreateNode();
        }
    }
}