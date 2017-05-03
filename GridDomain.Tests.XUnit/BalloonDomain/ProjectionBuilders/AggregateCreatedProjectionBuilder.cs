using System.Diagnostics;
using System.Threading.Tasks;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.CQRS.Messaging;
using GridDomain.Tests.XUnit.BalloonDomain.Events;

namespace GridDomain.Tests.XUnit.BalloonDomain.ProjectionBuilders
{
    public class AggregateCreatedProjectionBuilder : IHandlerWithMetadata<BalloonCreated>
    {
        public const string MessageProcessed = "message processed";
        public const string Why = "message received";

        private static readonly Stopwatch watch = new Stopwatch();

        private readonly IPublisher _publisher;

        static AggregateCreatedProjectionBuilder()
        {
            watch.Start();
        }

        public AggregateCreatedProjectionBuilder(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public static int ProjectionGroupHashCode { get; set; }

        public virtual Task Handle(BalloonCreated msg, IMessageMetadata metadata)
        {
            msg.History.SequenceNumber = int.Parse(msg.Value);
            msg.History.ElapsedTicksFromAppStart = watch.ElapsedTicks;

            var notificationMetadata = MessageMetadata.CreateFrom(msg.SourceId,
                                                                  metadata,
                                                                  new ProcessEntry(GetType().Name, MessageProcessed, Why));

            var notification = new AggregateCreatedEventNotification {AggregateId = msg.SourceId};
            _publisher.Publish(notification, notificationMetadata);

            return Task.CompletedTask;
        }

        public Task Handle(BalloonCreated msg)
        {
            return Handle(msg, MessageMetadata.Empty);
        }
    }
}