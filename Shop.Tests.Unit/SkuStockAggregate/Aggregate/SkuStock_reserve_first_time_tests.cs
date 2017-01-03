using System;
using System.Collections.Generic;
using System.Linq;
using GridDomain.Common;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.FutureEvents;
using GridDomain.Tests.Framework;
using NUnit.Framework;
using Shop.Domain.Aggregates.SkuStockAggregate;
using Shop.Domain.Aggregates.SkuStockAggregate.Commands;
using Shop.Domain.Aggregates.SkuStockAggregate.Events;

namespace Shop.Tests.Unit.SkuStockAggregate.Aggregate
{

    [TestFixture]
    class SkuStock_reserve_first_time_tests
    {
        private ReserveStockCommand _reserveStockCommand;
        private int _initialStock;
        private readonly TimeSpan _reserveTime = TimeSpan.FromMilliseconds(100);
        private Scenario<SkuStock, SkuStockCommandsHandler> _scenario;
        private DateTime _expirationDate;
        private Reservation _aggregateReserve;
        private FutureEventScheduledEvent _reserveExpirationFutureEvent;
        private ReserveExpired _reserveExpiredEvent;

        [SetUp]
        public void Given_sku_stock_with_amount_When_reserve_first_time()
        {
            Guid aggregateId = Guid.NewGuid();

            var reservationStartTime = BusinessDateTime.Now;

            _reserveStockCommand = new ReserveStockCommand(aggregateId,
                                                           Guid.NewGuid(), 
                                                           10, 
                                                           Guid.NewGuid(),
                                                           reservationStartTime);

            _expirationDate = reservationStartTime + _reserveTime;

            _scenario = Scenario.New<SkuStock, SkuStockCommandsHandler>()
                                .Given(new SkuStockCreated(aggregateId, Guid.NewGuid(), 50, _reserveTime),
                                       new StockAdded(aggregateId, 10, "test batch 2"))
                                .When(_reserveStockCommand);
                                
            _initialStock = _scenario.Aggregate.Quantity;
            _scenario.Run();
            _scenario.Aggregate.Reservations.TryGetValue(_reserveStockCommand.ReservationId, out _aggregateReserve);

            if(_aggregateReserve != null)
            _scenario.Aggregate.FutureEvents.TryGetValue(_aggregateReserve.Id, out _reserveExpirationFutureEvent);
            _reserveExpiredEvent = _reserveExpirationFutureEvent?.Event as ReserveExpired;
        }

        [Test]
        public void Then_stock_reserved_event_should_be_raised()
        {
            _scenario.Then(new StockReserved(_reserveStockCommand.StockId,
                                             _reserveStockCommand.ReservationId,
                                             _expirationDate,
                                             _reserveStockCommand.Quantity),
                           new FutureEventScheduledEvent(_reserveStockCommand.ReservationId,
                                                         _scenario.Aggregate.Id,
                                                         _expirationDate,
                                                         new ReserveExpired(_scenario.Aggregate.Id,
                                                                             _reserveStockCommand.ReservationId)));
            _scenario.Check();
        }

        [Test]
        public void Then_aggregate_reservation_should_be_added()
        {
           Assert.NotNull(_aggregateReserve);
        }

        [Test]
        public void Then_reservation_should_be_added_in_aggregate()
        {
            CollectionAssert.IsNotEmpty(_scenario.Aggregate.Reservations);
        }

        [Test]
        public void Then_aggregate_reservation_for_stock_should_have_correct_expiration_date()
        {
            Assert.AreEqual(_expirationDate, _aggregateReserve.ExpirationDate);
        }

        [Test]
        public void Reserve_expiration_future_event_should_exist()
        {
            Assert.NotNull(_reserveExpirationFutureEvent);
        }

        [Test]
        public void Reserve_expiration_future_event_should_have_reservation_id()
        {
            Assert.AreEqual(_aggregateReserve.Id,_reserveExpirationFutureEvent.Id);
        }

        [Test]
        public void Reserve_expiration_future_event_should_have_reservation_expiration_date()
        {
            Assert.AreEqual(_aggregateReserve.ExpirationDate, _reserveExpirationFutureEvent.RaiseTime);
        }

        [Test]
        public void Reserve_expiration_event_should_exist()
        {
            Assert.NotNull(_reserveExpiredEvent);
        }

        [Test]
        public void Reserve_expiration_event_should_have_reserve_id()
        {
            Assert.AreEqual(_aggregateReserve.Id,_reserveExpiredEvent?.ReserveId);
        }

        [Test]
        public void Then_aggregate_reservation_for_stock_should_have_correct_id_date()
        {
            Assert.AreEqual(_reserveStockCommand.ReservationId, _aggregateReserve.Id);
        }

        [Test]
        public void Then_aggregate_reservation_for_stock_should_have_correct_quanity()
        {
            Assert.AreEqual(_reserveStockCommand.Quantity, _aggregateReserve.Quantity);
        }

        [Test]
        public void Aggregate_quantity_should_be_decreased_by_command_amount()
        {
            Assert.AreEqual(_initialStock - _reserveStockCommand.Quantity, _scenario.Aggregate.Quantity);
        }
    }
}