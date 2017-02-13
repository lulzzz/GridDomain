using GridDomain.Node.Configuration.Composition;
using GridDomain.Tests.XUnit.FutureEvents.Infrastructure;
using Microsoft.Practices.Unity;
using Quartz;

namespace GridDomain.Tests.XUnit.FutureEvents
{
    internal class FutureEventsFixture : NodeTestFixture
    {
        public FutureEventsFixture()
        {
            Add(new CustomContainerConfiguration(c => c.RegisterAggregate<FutureEventsAggregate, FutureEventsAggregatesCommandHandler>()));
            Add(new FutureEventsRouteMap());
        }
        protected override void OnNodeStarted()
        {
            var scheduler = Node.Container.Resolve<IScheduler>();
            scheduler.Clear();
        }
    }
}