using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DI.Core;
using Akka.Monitoring;
using Akka.Monitoring.ApplicationInsights;
using Akka.Monitoring.PerformanceCounters;
using Akka.Util.Internal;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.CQRS.Messaging;
using GridDomain.CQRS.Messaging.Akka;
using GridDomain.CQRS.Messaging.Akka.Remote;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.Adapters;
using GridDomain.EventSourcing.CommonDomain;
using GridDomain.Node.Actors;
using GridDomain.Node.Actors.CommandPipe;
using GridDomain.Node.Configuration.Composition;
using Microsoft.Practices.Unity;
using IScheduler = Quartz.IScheduler;

namespace GridDomain.Node
{
    public class GridDomainNode : IGridDomainNode
    {
        private ICommandExecutor _commandExecutor;

        private IScheduler _quartzScheduler;
        private bool _stopping;
        private IMessageWaiterFactory _waiterFactory;
        internal CommandPipe Pipe;

        public GridDomainNode(NodeSettings settings)
        {
            Settings = settings;
        }

        public NodeSettings Settings { get; }
        public EventsAdaptersCatalog EventsAdaptersCatalog { get; private set; }
        public IActorTransport Transport { get; private set; }
        public ActorSystem System { get; private set; }
        private IActorRef ActorTransportProxy { get; set; }

        public IUnityContainer Container { get; private set; }

        public Guid Id { get; } = Guid.NewGuid();
        public event EventHandler<GridDomainNode> Initializing =  delegate {};

        public void Execute<T>(T command, IMessageMetadata metadata = null) where T : ICommand
        {
            _commandExecutor.Execute(command, metadata);
        }

        public IMessageWaiter<Task<IWaitResult>> NewExplicitWaiter(TimeSpan? defaultTimeout = null)
        {
            return _waiterFactory.NewExplicitWaiter(defaultTimeout ?? Settings.DefaultTimeout);
        }

        public ICommandWaiter Prepare<T>(T cmd, IMessageMetadata metadata = null) where T : ICommand
        {
            return _commandExecutor.Prepare(cmd, metadata);
        }

        public void Dispose()
        {
            Stop().Wait();
        }

        private void OnSystemTermination()
        {
            Settings.Log.Debug("grid node Actor system terminated");
        }

        public async Task Start()
        {
            await InitializeSystem();

            Container.Register(new GridNodeContainerConfiguration(System, Settings, Transport));
            Container.RegisterInstance(HandlersPipeActor.CustomHandlersProcessActorRegistrationName, Pipe.HandlersProcessor);
            Container.RegisterInstance(SagaPipeActor.SagaProcessActorRegistrationName, Pipe.SagaProcessor);

            _quartzScheduler = Container.Resolve<IScheduler>();

            InitMonitoring();

            Settings.Log.Debug("Launching GridDomain node {Id}", Id);

            await StartController();

            Settings.Log.Debug("GridDomain node {Id} started at home {Home}", Id, System.Settings.Home);
        }

        private async Task InitializeSystem()
        {
            _stopping = false;
            EventsAdaptersCatalog = new EventsAdaptersCatalog();
            Container = new UnityContainer();

            Initializing.Invoke(this, this);

            System = Settings.ActorSystemFactory();
            System.RegisterOnTermination(OnSystemTermination);
            System.InitDomainEventsSerialization(EventsAdaptersCatalog);
            Transport = new LocalAkkaEventBusTransport(System);
            ActorTransportProxy = System.ActorOf(Props.Create(() => new ActorTransportProxy(Transport)),
                                                 nameof(CQRS.Messaging.Akka.Remote.ActorTransportProxy));
            Pipe = new CommandPipe(System);
            _commandExecutor = new AkkaCommandPipeExecutor(System, Transport, await Pipe.Init(), Settings.DefaultTimeout);
            await new CompositeRouteMap("All settings map", Settings.Builder.MessageRouteMaps.ToArray()).Register(Pipe);
            _waiterFactory = new MessageWaiterFactory(System, Transport, Settings.DefaultTimeout);
        }

        private async Task StartController()
        {
            var props = System.DI()
                              .Props<GridNodeController>();
            var nodeController = System.ActorOf(props, nameof(GridNodeController));

            await nodeController.Ask<GridNodeController.Started>(new GridNodeController.Start());
        }

        private void InitMonitoring()
        {
            var appInsightsConfig = Container.Resolve<IAppInsightsConfiguration>();
            var perfCountersConfig = Container.Resolve<IPerformanceCountersConfiguration>();

            if (appInsightsConfig.IsEnabled)
            {
                var monitor = new ActorAppInsightsMonitor(appInsightsConfig.Key);
                ActorMonitoringExtension.RegisterMonitor(System, monitor);
            }
            if (perfCountersConfig.IsEnabled)
                ActorMonitoringExtension.RegisterMonitor(System, new ActorPerformanceCountersMonitor());
        }

        public async Task Stop()
        {
            if (_stopping)
                return;

            Settings.Log.Debug("GridDomain node {Id} is stopping", Id);
            _stopping = true;

            try
            {
                if (_quartzScheduler != null && _quartzScheduler.IsShutdown == false)
                    _quartzScheduler.Shutdown();
            }
            catch (Exception ex)
            {
                Settings.Log.Warning($"Got error on quartz scheduler shutdown:{ex}");
            }

            if (System != null)
            {
                await System.Terminate();
                System.Dispose();
            }
            System = null;
            Container?.Dispose();
            Settings.Log.Debug("GridDomain node {Id} stopped", Id);
        }

        public IMessageWaiter<Task<IWaitResult>> NewWaiter(TimeSpan? defaultTimeout = null)
        {
            return _waiterFactory.NewWaiter(defaultTimeout);
        }
    }
}