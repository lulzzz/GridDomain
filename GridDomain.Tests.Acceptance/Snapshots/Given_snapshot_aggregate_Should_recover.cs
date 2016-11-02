﻿using System;
using GridDomain.Tests.CommandsExecution;
using GridDomain.Tests.Framework;
using GridDomain.Tests.SampleDomain;
using GridDomain.Tools.Repositories;
using NUnit.Framework;

namespace GridDomain.Tests.Acceptance.Snapshots
{
    [TestFixture]
    class Given_snapshot_aggregate_Should_recover : SampleDomainCommandExecutionTests
    {
        private SampleAggregate _aggregate;
        private SampleAggregate _restoredAggregate;
        public Given_snapshot_aggregate_Should_recover(): base(false) {}

        [OneTimeSetUp]
        public void Test()
        {
            _aggregate = new SampleAggregate(Guid.NewGuid(), "test");
            _aggregate.ChangeState(10);
            _aggregate.ClearEvents();

            var repo = new AggregateSnapshotRepository(AkkaConf.Persistence.JournalConnectionString);
            repo.Add(_aggregate);

            _restoredAggregate = LoadAggregate<SampleAggregate>(_aggregate.Id);
        }

        [Test]
        public void Values_should_be_equal()
        {
            Assert.AreEqual(_aggregate.Value, _restoredAggregate.Value);
        }

        [Test]
        public void State_restored_from_sanapshot_should_not_have_uncommited_events()
        {
            CollectionAssert.IsEmpty(_restoredAggregate.GetEvents());
        }
        [Test]
        public void Ids_should_be_equal()
        {
            Assert.AreEqual(_aggregate.Id, _restoredAggregate.Id);
        }

    }
}
