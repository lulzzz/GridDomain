using System;
using System.Linq;
using System.Threading.Tasks;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.CQRS.Messaging;
using GridDomain.EventSourcing;
using GridDomain.Node.Actors;
using GridDomain.Node.AkkaMessaging.Waiting;
using GridDomain.Tests.Unit.BalloonDomain;
using GridDomain.Tests.Unit.BalloonDomain.Commands;
using GridDomain.Tests.Unit.BalloonDomain.Events;
using GridDomain.Tests.Unit.BalloonDomain.ProjectionBuilders;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Unit.Metadata
{
    public class Metadata_from_message_handler_event_passed_to_produced_fault : NodeTestKit
    {
        public Metadata_from_message_handler_event_passed_to_produced_fault(ITestOutputHelper output)
            : base(output, new NodeTestFixture(new BalloonDomainConfiguration(), CreateMap())) {}

        private IMessageMetadataEnvelop<IFault<BalloonCreated>> _answer;
        private InflateNewBallonCommand _command;
        private MessageMetadata _commandMetadata;
        private IMessageMetadataEnvelop<BalloonCreated> _aggregateEvent;

        private static IMessageRouteMap CreateMap()
        {
            return new CustomRouteMap(new BalloonRouteMap(),
                                      r =>
                                          r.RegisterHandler<BalloonCreated, BalloonCreatedFaultyProjection>(nameof(DomainEvent.SourceId)));
        }

        [Fact]
        public async Task When_execute_aggregate_command_with_metadata()
        {
            _command = new InflateNewBallonCommand(1, Guid.NewGuid());
            _commandMetadata = new MessageMetadata(_command.Id, BusinessDateTime.Now, Guid.NewGuid());

            var res = await Node.Prepare(_command, _commandMetadata)
                                .Expect<BalloonCreated>()
                                .And<Fault<BalloonCreated>>()
                                .Execute(null, false);

            _answer = res.MessageWithMetadata<IFault<BalloonCreated>>();
            _aggregateEvent = res.MessageWithMetadata<BalloonCreated>();
            //Result_contains_metadata()
            Assert.NotNull(_answer.Metadata);
            //Result_message_has_expected_type()
            Assert.IsAssignableFrom<IFault<BalloonCreated>>(_answer.Message);
            //Produced_fault_produced_by_projection_builder()
            Assert.Equal(typeof(BalloonCreatedFaultyProjection), _answer.Message.Processor);
            //Result_message_has_expected_id()
            Assert.Equal(_command.AggregateId, _answer.Message.Message.SourceId);
            //Result_metadata_has_aggregate_event_id_as_casuation_id()
            Assert.Equal(_aggregateEvent.Metadata.MessageId, _answer.Metadata.CasuationId);
            //Result_metadata_has_correlation_id_same_as_command_metadata()
            Assert.Equal(_commandMetadata.CorrelationId, _answer.Metadata.CorrelationId);
            //Result_metadata_has_processed_history_filled_from_aggregate()
            Assert.Equal(1, _answer.Metadata.History?.Steps.Count);
            //Result_metadata_has_processed_correct_filled_history_step()
            var step = _answer.Metadata.History.Steps.First();

            Assert.Equal(nameof(BalloonCreatedFaultyProjection), step.Who);
            Assert.Equal(MessageHandlingStatuses.MessageProcessCasuedAnError, step.Why);
            Assert.Equal(MessageHandlingStatuses.PublishingFault, step.What);
        }
    }
}