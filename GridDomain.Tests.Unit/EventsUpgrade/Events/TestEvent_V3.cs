using System;
using GridDomain.EventSourcing;

namespace GridDomain.Tests.Unit.EventsUpgrade.Events
{
    public class TestEvent_V3 : DomainEvent
    {
        public TestEvent_V3(string sourceId, DateTime? createdTime = null, string processId = null)
            : base(sourceId, processId: processId, createdTime: createdTime) {}

        public TestEvent_V3() : this(null) {}

        public int Field4 { get; set; }
    }
}