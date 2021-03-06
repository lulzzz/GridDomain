using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DI.Core;
using Akka.Persistence;
using Akka.TestKit.Xunit2;
using GridDomain.EventSourcing;
using GridDomain.Node.Actors;
using GridDomain.Node.Actors.Aggregates;
using GridDomain.Node.Actors.EventSourced.Messages;
using GridDomain.Node.AkkaMessaging;
using GridDomain.ProcessManagers;
using GridDomain.ProcessManagers.State;

namespace GridDomain.Tests.Unit
{
    public static class TestKitExtensions
    {
        public static async Task<T> LoadAggregateByActor<T>(this TestKit kit, string id) where T : Aggregate
        {
            var name = EntityActorName.New<T>(id).ToString();
            var actor = await kit.LoadActor<AggregateActor<T>>(name);
            return actor.State;
        }

        public static async Task<T> LoadActor<T>(this TestKit kit, string name) where T : ActorBase
        {
            var diActorSystemAdapter = kit.Sys.DI();
            var props = diActorSystemAdapter.Props<T>();

            var actor = kit.ActorOfAsTestActorRef<T>(props, name);

            await actor.Ask<RecoveryCompleted>(NotifyOnPersistenceEvents.Instance,TimeSpan.FromSeconds(5));

            return actor.UnderlyingActor;
        }

        public static async Task<TState> LoadProcessByActor<TState>(this TestKit kit, string id)
            where TState : class, IProcessState
        {
            return (await kit.LoadAggregateByActor<ProcessStateAggregate<TState>>(id)).State;
        }
    }
}