using GridDomain.Common;
using GridDomain.CQRS.Messaging.Akka;
using GridDomain.Node;
using GridDomain.Node.Configuration.Composition;
using GridDomain.Node.Configuration.Persistence;
using Helios.Util;
using Microsoft.Practices.Unity;
using Serilog;
using Xunit.Abstractions;

namespace GridDomain.Tests.Unit
{
    public class GridNodeContainerTests : CompositionRootTests
    {
        private readonly ILogger _logger;

        public GridNodeContainerTests(ITestOutputHelper output)
        {
            _logger = new XUnitAutoTestLoggerConfiguration(output).CreateLogger();
        }

        protected override IUnityContainer CreateContainer(TransportMode mode, IDbConfiguration conf)
        {
            var container = new UnityContainer();

            var actorSystem = ActorSystemBuilders[mode]();
            NodeSettings settings = new NodeSettings(null);
            container.Register(new GridNodeContainerConfiguration(new LocalAkkaEventBusTransport(actorSystem), settings.Log));
            actorSystem.Terminate();
            return container;
        }
    }
}