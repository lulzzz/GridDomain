using GridDomain.Configuration.MessageRouting;
using GridDomain.ProcessManagers;
using GridDomain.ProcessManagers.State;

namespace GridDomain.Configuration {
    public class ProcessStateDependencyFactory<TState> : DefaultAggregateDependencyFactory<ProcessStateAggregate<TState>> where TState : IProcessState
    {
        public ProcessStateDependencyFactory():base(() => new ProcessStateCommandHandler<TState>(), () => EmptyRouteMap.Instance)
        {
        }
    }
}