﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using GridDomain.EventSourcing;
using GridDomain.Tests.Framework.Configuration;
using GridDomain.Tests.SampleDomain;
using GridDomain.Tests.SampleDomain.Events;
using GridDomain.Tools;
using NUnit.Framework;

namespace GridDomain.Tests.Tools
{
    [TestFixture]
    class Test_event_repositoty_tools
    {
        private Guid _sourceId;

        private class Message
        {
            public int Id;
        }

        private class Message2 : Message
        {
        }

        [Test]
        public void Given_events_When_save_by_events_repository_Then_events_can_be_fetched()
        {
            var events = new []
            {
                new Message()  {Id = 1}, 
                new Message2() {Id = 2}
            };

            using (var repo = CreateRepository())
            {
                var persistId = "testId";

                repo.Save(persistId,events);
                var eventsLoaded = repo.Load(persistId).Cast<Message>();
                CollectionAssert.AreEquivalent(events.Select(e => e.Id),eventsLoaded.Select(e=> e.Id));
            }
        }

        protected virtual AkkaEventRepository CreateRepository()
        {
            return new AkkaEventRepository(new AutoTestAkkaConfiguration().ToStandAloneSystemConfig());
        }
    }
}
