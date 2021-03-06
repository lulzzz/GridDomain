using System;
using GridDomain.Configuration.MessageRouting;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.CommonDomain;

namespace GridDomain.Configuration {
    public static class DefaultAggregateDependencyFactory
    {
        public static DefaultAggregateDependencyFactory<TAggregate> New<TAggregate>(IAggregateCommandsHandler<TAggregate> handler, IMessageRouteMap mapProducer=null) where TAggregate : IAggregate
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var map = mapProducer ?? MessageRouteMap.New(handler);

            return new DefaultAggregateDependencyFactory<TAggregate>(() => handler,() => map);
        }
        
        public static DefaultAggregateDependencyFactory<TAggregate> New<TAggregate, TAggregateCommandsHandler>()
            where TAggregate : IAggregate
            where TAggregateCommandsHandler : class, IAggregateCommandsHandler<TAggregate>, new()
        {
            return New(new TAggregateCommandsHandler());
        }

        public static DefaultAggregateDependencyFactory<TCommandAggregate> ForCommandAggregate<TCommandAggregate>(IConstructAggregates factory = null) where TCommandAggregate : CommandAggregate
        {
            var depFactory = new DefaultAggregateDependencyFactory<TCommandAggregate>(() => CommandAggregateHandler.New<TCommandAggregate>(factory));
            if (factory != null)
                depFactory.AggregateFactoryCreator = () => factory;
            return depFactory;
        }
    }


    public class DefaultAggregateDependencyFactory<TAggregate> : IAggregateDependencyFactory<TAggregate> where TAggregate : IAggregate
    {
        public Func<IMessageRouteMap> MapProducer { protected get; set; }
        public Func<IAggregateCommandsHandler<TAggregate>> HandlerCreator { protected get; set; }
        public Func<ISnapshotsPersistencePolicy> SnapshotPolicyCreator { protected get; set; }
        public Func<IConstructAggregates> AggregateFactoryCreator { protected get; set; }
        public Func<IConstructSnapshots> SnapshotsFactoryCreator { protected get; set; }
        public Func<IRecycleConfiguration> RecycleConfigurationCreator { protected get; set; }

        public DefaultAggregateDependencyFactory(Func<IAggregateCommandsHandler<TAggregate>> handler, 
                                                 Func<IMessageRouteMap> mapProducer = null)
        {
            MapProducer = mapProducer ?? (() => MessageRouteMap.New(handler()));
            HandlerCreator = handler ?? throw new ArgumentNullException(nameof(handler));
            SnapshotPolicyCreator = () => new NoSnapshotsPersistencePolicy();
            AggregateFactoryCreator = () => AggregateFactory.Default;
            SnapshotsFactoryCreator = () => AggregateFactory.Default;
            RecycleConfigurationCreator = () => new DefaultRecycleConfiguration();
        }

       

        public virtual IAggregateCommandsHandler<TAggregate> CreateCommandsHandler()
        {
            return HandlerCreator();
        }

        public virtual ISnapshotsPersistencePolicy CreatePersistencePolicy()
        {
            return SnapshotPolicyCreator();
        }

        public virtual IConstructAggregates CreateAggregateFactory()
        {
            return AggregateFactoryCreator();
        }

        public IConstructSnapshots CreateSnapshotsFactory()
        {
            return SnapshotsFactoryCreator();
        }

        public virtual IRecycleConfiguration CreateRecycleConfiguration()
        {
            return RecycleConfigurationCreator();
        }

        public IMessageRouteMap CreateRouteMap()
        {
            return MapProducer();
        }
    }
}