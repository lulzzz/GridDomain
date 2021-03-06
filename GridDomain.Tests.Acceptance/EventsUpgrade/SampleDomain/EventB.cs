using System;
using GridDomain.EventSourcing;

namespace GridDomain.Tests.Acceptance.EventsUpgrade.SampleDomain
{
    internal class EventB : DomainEvent
    {
        public EventB(string sourceId, IOrder order) : base(sourceId)
        {
            Order = order;
        }

        public IOrder Order { get; }
    }
}