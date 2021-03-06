using System;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Event;
using GridDomain.Common;

namespace GridDomain.Transport
{
    public class DistributedPubSubTransport : IActorTransport
    {
        private readonly ILoggingAdapter _log;
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);
        private readonly IActorRef _transport;

        public DistributedPubSubTransport(ActorSystem system)
        {
            _log = system.Log;

            DistributedPubSub distributedPubSub;
            try
            {
                distributedPubSub = DistributedPubSub.Get(system);
            }
            catch (Exception ex)
            {
                throw new CannotGetDistributedPubSubException(ex);
            }
            if (distributedPubSub == null)
                throw new CannotGetDistributedPubSubException();

            _transport = distributedPubSub.Mediator;
        }

        public void Subscribe<TMessage>(IActorRef actor)
        {
            Subscribe(typeof(TMessage), actor, actor);
        }

        public void Unsubscribe(IActorRef actor, Type topic)
        {
            _transport.Ask<UnsubscribeAck>(new Unsubscribe(topic.FullName, actor), _timeout);
        }

        public void Subscribe(Type messageType, IActorRef actor, IActorRef subscribeNotificationWaiter)
        {
            var topic = messageType.FullName;
            //TODO: replace wait with actor call
            var ack = _transport.Ask<SubscribeAck>(new Subscribe(topic, actor), _timeout).Result;
            subscribeNotificationWaiter.Tell(ack);
            _log.Debug("Subscribing handler actor {Path} to topic {Topic}", actor.Path, topic);
        }

        public void Publish(object msg)
        {
            var topic = msg.GetType().FullName;
            _transport.Tell(new Publish(topic, msg));
        }

        public void Publish(object msg, IMessageMetadata metadata)
        {
            _transport.Tell(new Publish(msg.GetType().FullName, new MessageMetadataEnvelop(msg, metadata)));
        }
    }
}