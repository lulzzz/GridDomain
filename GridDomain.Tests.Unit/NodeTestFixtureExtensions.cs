using Akka.TestKit.TestActors;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.Adapters;
using GridDomain.Node.Actors.CommandPipe.Messages;
using Microsoft.Practices.Unity;
using Akka.Actor;
using GridDomain.Scheduling;

namespace GridDomain.Tests.Unit
{
    public static class NodeTestFixtureExtensions
    {
        public static void ClearSheduledJobs(this NodeTestFixture fixture)
        {
            fixture.OnNodeStartedEvent += (sender, args) => 
            ((ExtendedActorSystem)fixture.Node.System).GetExtension<SchedulingExtension>().Scheduler.Clear();
        }

        public static NodeTestFixture UseAdaper<TFrom,TTo>(this NodeTestFixture fixture, ObjectAdapter<TFrom,TTo> adapter)
        {
            fixture.OnNodeCreatedEvent += (sender, node) => node.EventsAdaptersCatalog.Register(adapter);
            return fixture;
        }

        public static NodeTestFixture UseAdaper<TFrom,TTo>(this NodeTestFixture fixture, DomainEventAdapter<TFrom,TTo> adapter) where TFrom : DomainEvent
                                                                                                                                where TTo : DomainEvent
        {
            fixture.OnNodeCreatedEvent += (sender, node) => node.EventsAdaptersCatalog.Register(adapter);
            return fixture;
        }

        public static NodeTestFixture IgnoreCommands(this NodeTestFixture fixture)
        {
            fixture.OnNodeStartedEvent += (sender, e) =>
                                          {
                                              //supress errors raised by commands not reaching aggregates
                                              var nullActor = fixture.Node.System.ActorOf(BlackHoleActor.Props);
                                              fixture.Node.Pipe.SagaProcessor.Ask<Initialized>(new Initialize(nullActor))
                                                     .Wait();
                                          };

            return fixture;
        }
    }
}