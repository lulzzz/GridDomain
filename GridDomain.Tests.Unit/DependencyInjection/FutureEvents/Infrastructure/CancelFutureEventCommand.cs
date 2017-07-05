using System;
using GridDomain.CQRS;

namespace GridDomain.Tests.Unit.DependencyInjection.FutureEvents.Infrastructure
{
    public class CancelFutureEventCommand : Command
    {
        public CancelFutureEventCommand(Guid aggregateId, string value) : base(aggregateId)
        {
            Value = value;
        }

        public string Value { get; }
    }
}