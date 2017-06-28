using System;
using GridDomain.CQRS.Messaging;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.CommonDomain;
using GridDomain.EventSourcing.Sagas;
using GridDomain.EventSourcing.Sagas.InstanceSagas;

namespace GridDomain.Node.Configuration.Composition {
    public interface ISagaDependencyFactory<TProcess, TState>: IRouteMapFactory where TState : class, ISagaState
                                                              where TProcess : Process<TState>
    {
        ISaga—reatorCatalog<TState> CreateCatalog();
        IAggregateDependencyFactory<SagaStateAggregate<TState>> StateDependencyFactory { get; }
    }
    
    public class DefaultSagaDependencyFactory<TProcess, TState> : ISagaDependencyFactory<TProcess, TState>
        where TState : class, ISagaState
        where TProcess : Process<TState>
    {
        private readonly ISaga—reatorCatalog<TState> _saga—reatorCatalog;
        public Func<IMessageRouteMap> RouteMapCreator { get; set; }
       
        public DefaultSagaDependencyFactory(ISaga—reatorCatalog<TState> catalog)
        {
            _saga—reatorCatalog = catalog;
        }
        public DefaultSagaDependencyFactory(ISagaCreator<TState> creator, ISagaDescriptor descriptor):this(BuildCatalog(creator,descriptor))
        {
            RouteMapCreator = () => MessageRouteMap.New(descriptor);
        }

        public ISaga—reatorCatalog<TState> CreateCatalog()
        {
            return _saga—reatorCatalog;
        }

        private static Saga—reatorsCatalog<TState> BuildCatalog(ISagaCreator<TState> factoryCreator, ISagaDescriptor descriptor)
        {
            var producer = new Saga—reatorsCatalog<TState>(descriptor, factoryCreator);
            producer.RegisterAll(factoryCreator);
            return producer;
        }
     
        public virtual SagaStateDependencyFactory<TState> StateDependencyFactory { get; } = new SagaStateDependencyFactory<TState>();

        IAggregateDependencyFactory<SagaStateAggregate<TState>> ISagaDependencyFactory<TProcess, TState>.StateDependencyFactory => StateDependencyFactory;
        public virtual IMessageRouteMap CreateRouteMap()
        {
            return RouteMapCreator();
        }
    }

    public class SagaStateDependencyFactory<TState> : DefaultAggregateDependencyFactory<SagaStateAggregate<TState>> where TState : ISagaState
    {
        public SagaStateDependencyFactory():base(() => new SagaStateCommandHandler<TState>(), () => EmptyRouteMap.Instance)
        {
        }
    }

}