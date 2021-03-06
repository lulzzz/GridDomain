using System;
using GridDomain.Configuration.MessageRouting;
using GridDomain.CQRS;

namespace GridDomain.Configuration {

    public class MessageHandlerFactory<TMessage, THandler> : IMessageHandlerFactory<TMessage,THandler>
                                                             where THandler : IHandler<TMessage>
    {
        private readonly Func<IMessageProcessContext, THandler> _handlerCreator;
        private readonly Func<IMessageRouteMap> _mapCreator;

        public MessageHandlerFactory(Func<IMessageProcessContext, THandler> handlerCreator, Func<IMessageRouteMap> mapCreator)
        {
            _mapCreator = mapCreator;
            _handlerCreator = handlerCreator;
        }

        public THandler Create(IMessageProcessContext context)
        {
            return _handlerCreator(context);
        }

        public  IMessageRouteMap CreateRouteMap()
        {
            return _mapCreator();
        }
    }
}