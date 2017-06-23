﻿using System.Collections.Generic;
using System.Threading.Tasks;
using GridDomain.EventSourcing;
using GridDomain.Tests.Common;
using NMoneys;
using Shop.Domain.Aggregates.SkuAggregate;
using Shop.Domain.Aggregates.SkuAggregate.Commands;
using Shop.Domain.Aggregates.SkuAggregate.Events;
using Shop.Infrastructure;
using Xunit;

namespace Shop.Tests.Unit.XUnit.SkuAggregate.Aggregate
{
   
    internal class Sku_creation_tests : AggregateCommandsTest<Sku, SkuCommandsHandler>
    {
        private static readonly InMemorySequenceProvider SequenceProvider = new InMemorySequenceProvider();
        private CreateNewSkuCommand _cmd;

        protected override SkuCommandsHandler CreateCommandsHandler()
        {
            return new SkuCommandsHandler(SequenceProvider);
        }

        protected override IEnumerable<DomainEvent> Expected()
        {
            yield return new SkuCreated(Aggregate.Id, _cmd.Name, _cmd.Article, 1, new Money(100));
        }

       [Fact]
        public async Task When_creating_new_sku()
        {
            Init();
            _cmd = new CreateNewSkuCommand("testSku", "tesstArticle", Aggregate.Id, new Money(100));
            await Execute(_cmd);
        }
    }
}