using System;
using Akka.Actor;
using Akka.TestKit.TestActors;
using GridDomain.Node.Actors;
using GridDomain.Node.Actors.CommandPipe;
using GridDomain.Node.Configuration.Composition;
using GridDomain.Tests.Common;
using GridDomain.Tests.Unit;
using GridDomain.Tests.Unit.BalloonDomain;
using GridDomain.Tests.Unit.Sagas.SoftwareProgrammingDomain;
using Microsoft.Practices.Unity;

namespace GridDomain.Tests.Acceptance.EventsUpgrade
{
    public static class TestFixtureExtensions
    {
        public static void InitFastRecycle(this NodeTestFixture fixture,
                                           TimeSpan? clearPeriod = null,
                                           TimeSpan? maxInactiveTime = null)
        {
            fixture.Add(
                        new CustomContainerConfiguration(c =>
                                                             c.RegisterInstance<IPersistentChildsRecycleConfiguration>(
                                                                                                                       new PersistentChildsRecycleConfiguration(clearPeriod ?? TimeSpan.FromMilliseconds(200),
                                                                                                                                                                maxInactiveTime ?? TimeSpan.FromMilliseconds(50)))));
        }

        public static NodeTestFixture InitSampleAggregateSnapshots(this NodeTestFixture fixture,
                                                                   int keep = 1,
                                                                   TimeSpan? maxSaveFrequency = null)
        {
            fixture.Add(AggregateConfiguration.New<Balloon, BalloonCommandHandler>(
                                                                                   () => new SnapshotsPersistencePolicy(1, keep, maxSaveFrequency)
                                                                                         {
                                                                                             Log = fixture.Logger.ForContext<SnapshotsPersistencePolicy>()
                                                                                         },
                                                                                   Balloon.FromSnapshot));

            return fixture;
        }

        public static NodeTestFixture InitSoftwareProgrammingSagaSnapshots(this NodeTestFixture fixture,
                                                                           int keep = 1,
                                                                           TimeSpan? maxSaveFrequency = null,
                                                                           int saveOnEach = 1)
        {
            var containerConfiguration = SagaConfiguration.New<SoftwareProgrammingProcess,
                                                               SoftwareProgrammingState,
                                                               SoftwareProgrammingSagaFactory>
                (SoftwareProgrammingProcess.Descriptor,
                 () => new SnapshotsPersistencePolicy(saveOnEach, keep, maxSaveFrequency));

            fixture.Add(new CustomContainerConfiguration(c =>{c.Register(containerConfiguration);}));

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