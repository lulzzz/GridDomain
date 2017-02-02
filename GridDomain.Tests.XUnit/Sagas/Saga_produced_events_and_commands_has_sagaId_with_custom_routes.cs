using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.Sagas;
using GridDomain.Node.Actors.CommandPipe;
using GridDomain.Node.AkkaMessaging.Waiting;
using GridDomain.Tests.Framework;
using GridDomain.Tests.XUnit.Sagas.SoftwareProgrammingDomain;
using GridDomain.Tests.XUnit.Sagas.SoftwareProgrammingDomain.Commands;
using GridDomain.Tests.XUnit.Sagas.SoftwareProgrammingDomain.Events;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.XUnit.Sagas
{
    public class Saga_produced_events_and_commands_has_sagaId_with_custom_routes : NodeTestKit,
                                                                                   IClassFixture<CustomRoutesSampleDomainFixture>
    {
        public Saga_produced_events_and_commands_has_sagaId_with_custom_routes(CustomRoutesSampleDomainFixture fixture, ITestOutputHelper helper) 
            : base(helper, fixture)
        {

        }

        [Fact]
        public async Task When_dispatch_command_than_command_should_have_right_sagaId()
        { 

            await Node.Pipe.SetCommandActorForSagas(TestActor);

            var domainEvent = new GotTiredEvent(Guid.NewGuid());
            Node.Pipe.SagaProcessor.Tell(new MessageMetadataEnvelop<DomainEvent[]>(new[] { domainEvent },
                                                                                        MessageMetadata.Empty));

            var sagaCompleteMsg = FishForMessage<IMessageMetadataEnvelop<ICommand>>(m => true);
            var command = sagaCompleteMsg.Message;

            Assert.Equal(domainEvent.PersonId, command.SagaId);
            Assert.IsAssignableFrom<MakeCoffeCommand>(command);
        }

        [Fact]
        public async Task When_raise_saga_than_saga_created_event_should_have_right_sagaId()
        {
            await Node.Pipe.SetCommandActorForSagas(TestActor);

            var gotTiredEvent = new GotTiredEvent(Guid.NewGuid());
            var waitResults = await Node.NewDebugWaiter()
                                        .Expect<SagaCreatedEvent<SoftwareProgrammingSagaData>>()
                                        .Create()
                                        .SendToSagas(gotTiredEvent);

            var sagaCreatedEvent = waitResults.Message<SagaCreatedEvent<SoftwareProgrammingSagaData>>();

            Assert.Equal(gotTiredEvent.PersonId, sagaCreatedEvent.SagaId);
        }

    }
}