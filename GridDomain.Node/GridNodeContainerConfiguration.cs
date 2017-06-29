using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DI.Core;
using Akka.DI.Unity;
using Akka.Util.Internal;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.CQRS.Messaging;
using GridDomain.CQRS.Messaging.Akka;
using GridDomain.Node.Actors;
using GridDomain.Node.AkkaMessaging;
using GridDomain.Node.Configuration.Composition;
using GridDomain.Scheduling;
using GridDomain.Scheduling.Integration;
using GridDomain.Scheduling.Quartz;
using GridDomain.Scheduling.Quartz.Retry;
using Microsoft.Practices.Unity;
using Serilog;

namespace GridDomain.Node
{
    public class GridNodeContainerConfiguration : IContainerConfiguration
    {
        private readonly ActorSystem _actorSystem;
        private readonly NodeSettings _settings;
        private readonly IActorTransport _actorTransport;

        public GridNodeContainerConfiguration(ActorSystem actorSystem, NodeSettings settings, IActorTransport transport)
        {
            _actorTransport = transport;
            _actorSystem = actorSystem;
            _settings = settings;
        }

        public void Register(IUnityContainer container)
        {
            _actorSystem.AddDependencyResolver(new UnityDependencyResolver(container, _actorSystem));

            _settings.Builder.ContainerConfigurations.ForEach(container.Register);

            container.RegisterInstance(_settings.Log);
            container.RegisterInstance<IPublisher>(_actorTransport);
            container.RegisterInstance<IActorSubscriber>(_actorTransport);
            container.RegisterInstance(_actorTransport);
            container.RegisterInstance<IMessageProcessContext>(new MessageProcessContext(_actorTransport, _settings.Log));
            container.RegisterType<IHandlerActorTypeFactory, DefaultHandlerActorTypeFactory>();
            container.RegisterInstance(AppInsightsConfigSection.Default ?? new DefaultAppInsightsConfiguration());
            container.RegisterInstance(PerformanceCountersConfigSection.Default ?? new DefaultPerfCountersConfiguration());
            container.RegisterInstance((IRetrySettings) _settings.QuartzJobRetrySettings);

            new QuartzSchedulerConfiguration(_settings.QuartzConfig).Register(container);
            var persistentScheduler = _actorSystem.ActorOf(_actorSystem.DI().Props<SchedulingActor>(),
                                                           nameof(SchedulingActor));

            container.RegisterInstance(SchedulingActor.RegistrationName, persistentScheduler);

        }
    }
}