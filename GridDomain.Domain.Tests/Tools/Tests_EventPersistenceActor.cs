﻿using System.Linq;
using Akka.Actor;
using Akka.TestKit.NUnit;
using GridDomain.Tools;
using NUnit.Framework;

namespace GridDomain.Tests.Tools
{
    [TestFixture]
    public class EventPersistenceActor_Tests : TestKit
    {

        [Test]
        public void When_actor_is_created_it_does_not_send_anything()
        {
            var actor = CreateActor("1");
            ExpectNoMsg(500);
        }


        [Test]
        public void When_actor_is_created_and_asked_for_load_response_is_empty()
        {
            var actor = CreateActor("1");
            var res = LoadEvents(actor);
            CollectionAssert.IsEmpty(res.Events);
        }

        [Test]
        public void When_actor_is_created_and_asked_for_load_response_contains_persisteneId()
        {
            var actor = CreateActor("1");
            var res = LoadEvents(actor);
            Assert.AreEqual("1",res.PersistenceId);
        }

        [Test]
        public void When_acor_receives_persist_request_it_responses_with_initial_persisted_payload()
        {
            var actor = CreateActor("2");
            var payload = "123";
            var persisted = Save(actor, payload);
            Assert.AreEqual(payload, persisted.Payload);
        }

        [Test]
        public void When_acor_receives_persist_request_it_persist_payload()
        {
            var actor = CreateActor("2");
            var payload = "123";
            Save(actor, payload);

            var loaded = LoadEvents(actor);
            Assert.AreEqual(payload, loaded.Events.First());
        }

        private IActorRef CreateActor(string persistenceId)
        {
            return Sys.ActorOf(Props.Create(() => new EventsRepositoryActor(persistenceId)));
        }

        private EventsRepositoryActor.Loaded LoadEvents(IActorRef actor)
        {
            actor.Tell(new EventsRepositoryActor.Load());
            var res = ExpectMsg<EventsRepositoryActor.Loaded>();
            return res;
        }

        private EventsRepositoryActor.Persisted Save(IActorRef actor, object payload)
        {
            actor.Tell(new EventsRepositoryActor.Persist(payload));
            var persisted = ExpectMsg<EventsRepositoryActor.Persisted>();
            return persisted;
        }
    }
}