﻿using System;
using System.Threading.Tasks;
using GridDomain.CQRS;
using GridDomain.Node.AkkaMessaging.Waiting;
using GridDomain.Tests.Acceptance.XUnit.EventsUpgrade;
using GridDomain.Tests.Framework;
using GridDomain.Tests.XUnit;
using GridDomain.Tests.XUnit.BalloonDomain;
using GridDomain.Tests.XUnit.BalloonDomain.Events;
using GridDomain.Tools.Repositories.AggregateRepositories;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Acceptance.XUnit.Snapshots
{
    public class Aggregate_should_recover_from_snapshot : NodeTestKit
    {
        public Aggregate_should_recover_from_snapshot(ITestOutputHelper output)
            : base(output, new BalloonFixture {InMemory = false}.InitSampleAggregateSnapshots()) {}

        [Fact]
        public async Task Test()
        {
            var aggregate = new Balloon(Guid.NewGuid(), "test");
            aggregate.WriteNewTitle(10);
            aggregate.ClearEvents();

            var repo = new AggregateSnapshotRepository(AkkaConfig.Persistence.JournalConnectionString,
                                                       Node.AggregateFromSnapshotsFactory);
            await repo.Add(aggregate);

            var cmd = new IncreaseTitleCommand(1, aggregate.Id);

            var res = await Node.Prepare(cmd).Expect<BalloonTitleChanged>().Execute();

            var message = res.Message<BalloonTitleChanged>();

            //Values_should_be_equal()
            Assert.Equal("11", message.Value);
            //Ids_should_be_equal()
            Assert.Equal(aggregate.Id, message.SourceId);
        }
    }
}