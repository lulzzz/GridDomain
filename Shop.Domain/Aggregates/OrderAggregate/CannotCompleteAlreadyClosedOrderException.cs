using GridDomain.EventSourcing;

namespace Shop.Domain.Aggregates.OrderAggregate
{
    public class CannotCompleteAlreadyClosedOrderException : DomainException
    {
    }
}