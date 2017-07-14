using System;
using GridDomain.EventSourcing;

namespace GridDomain.Tests.Unit.DependencyInjection.FutureEvents.Infrastructure
{
    public class ValueChangedSuccessfullyEvent : DomainEvent
    {
        public string Value { get; }
        public int RetriesToSucceed { get; }

        public ValueChangedSuccessfullyEvent(string value,
                               int retriesToSucceed,
                               Guid sourceId,
                               DateTime? createdTime = default(DateTime?),
                               Guid sagaId = default(Guid)) : base(sourceId, sagaId, null, createdTime)
        {
            Value = value;
            RetriesToSucceed = retriesToSucceed;
        }

    }
}