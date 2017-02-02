using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.Sagas.InstanceSagas;
using GridDomain.Node.Actors;
using GridDomain.Node.Actors.CommandPipe;
using GridDomain.Node.AkkaMessaging;
using GridDomain.Tests.Unit.Sagas.InstanceSagas;
using GridDomain.Tests.Unit.Sagas.SoftwareProgrammingDomain.Commands;
using GridDomain.Tests.Unit.Sagas.SoftwareProgrammingDomain.Events;
using NUnit.Framework;

namespace GridDomain.Tests.Unit
{
    [TestFixture]
    class Metadata_from_saga_received_event_passed_to_produced_commands : SoftwareProgrammingInstanceSagaTest
    {
        private Guid SagaId;
        private IMessageMetadataEnvelop<ICommand> _answer;
        private MakeCoffeCommand command; 
        private GotTiredEvent _gotTiredEvent;
        private MessageMetadata _gotTiredEventMetadata;

        [OneTimeSetUp]
        public void When_publishing_start_message()
        {
            SagaId = Guid.NewGuid();
            _gotTiredEvent = new GotTiredEvent(Guid.NewGuid(), Guid.NewGuid(),Guid.NewGuid(), SagaId);
            _gotTiredEventMetadata = new MessageMetadata(_gotTiredEvent.SourceId, BusinessDateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid());

            GridNode.Pipe.SagaProcessor.Tell(new Initialize(TestActor));
            GridNode.Pipe.SagaProcessor.Tell(new MessageMetadataEnvelop<DomainEvent[]>(new []{ _gotTiredEvent }, _gotTiredEventMetadata));

            _answer = FishForMessage<IMessageMetadataEnvelop<ICommand>>(m => true);
            command = _answer.Message as MakeCoffeCommand;
        }


        [Test]
        public void Result_contains_metadata()
        {
            Assert.NotNull(_answer.Metadata);
        }

        [Test]
        public void Result_contains_message()
        {
            Assert.NotNull(_answer.Message);
        }

        [Test]
        public void Result_message_has_expected_type()
        {
            Assert.IsInstanceOf<MakeCoffeCommand>(_answer.Message);
        }

        [Test]
        public void Result_message_has_expected_value()
        {
            Assert.AreEqual(_gotTiredEvent.PersonId, command.PersonId);
        }

        [Test]
        public void Result_metadata_has_command_id_as_casuation_id()
        {
            Assert.AreEqual(_gotTiredEvent.SourceId, _answer.Metadata.CasuationId);
        }

        [Test]
        public void Result_metadata_has_correlation_id_same_as_command_metadata()
        {
            Assert.AreEqual(_gotTiredEventMetadata.CorrelationId, _answer.Metadata.CorrelationId);
        }

        [Test]
        public void Result_metadata_has_processed_history_filled_from_aggregate()
        {
            Assert.AreEqual(1, _answer.Metadata.History?.Steps.Count);
        }

        [Test]
        public void Result_metadata_has_processed_correct_filled_history_step()
        {
            var step = _answer.Metadata.History.Steps.First();
            var name = AggregateActorName.New<SagaStateAggregate<SoftwareProgrammingSagaData>>(SagaId);

            Assert.AreEqual(name.Name, step.Who);
            Assert.AreEqual(SagaActorLiterals.SagaProducedACommand, step.Why);
            Assert.AreEqual(SagaActorLiterals.PublishingCommand, step.What);
        }
    }
}