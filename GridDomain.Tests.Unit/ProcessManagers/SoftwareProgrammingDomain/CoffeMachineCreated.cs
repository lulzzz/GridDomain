using System;
using GridDomain.EventSourcing;

namespace GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain {
    public class CoffeMachineCreated : DomainEvent
    {
        public int MaxCups { get; }

        public CoffeMachineCreated(string id, int maxCups):base(id)
        {
            MaxCups = maxCups;
        }
    }
}