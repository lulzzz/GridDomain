using Akka.Actor;
using Akka.Event;
using GridDomain.CQRS;
using GridDomain.Logging;
using NLog;

namespace GridDomain.Node.AkkaMessaging
{
    public class MessageHandlingActor<TMessage, THandler> : UntypedActor where THandler : IHandler<TMessage>
    {
        private readonly THandler _handler;
        private readonly ILoggingAdapter _log = Context.GetLogger();

        public MessageHandlingActor(THandler handler)
        {
            _handler = handler;
            _log.Debug($"Created message handler actor {GetType()}");
        }

        protected override void OnReceive(object msg)
        {
            _log.Debug($"Handler actor got message: {msg.ToPropsString()}");
            _handler.Handle((TMessage) msg);
        }
    }
}