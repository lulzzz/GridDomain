using System;

namespace GridDomain.EventSourcing.Sagas
{
    public class SagaCreated<TState> : SagaStateEvent
    {
        public SagaCreated(TState state, Guid sourceId) : base(sourceId)
        {
            State = state;
        }

        public TState State { get; }
    }
}