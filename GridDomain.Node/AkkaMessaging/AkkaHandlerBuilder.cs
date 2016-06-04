using GridDomain.CQRS;
using GridDomain.CQRS.Messaging.MessageRouting;

namespace GridDomain.Node.AkkaMessaging
{
    public class AkkaHandlerBuilder<TMessage, THandler> :
        IHandlerBuilder<TMessage, THandler> where THandler : IHandler<TMessage>
    {
        private readonly IHandler<CreateHandlerRoute> _routingRegistrator;
        public string CorrelationPropertyName;

        public AkkaHandlerBuilder(IHandler<CreateHandlerRoute> routingRegistrator)
        {
            _routingRegistrator = routingRegistrator;
        }

        public void Register()
        {
            var createHandlerRoute = CreateHandlerRoute.New<TMessage, THandler>(CorrelationPropertyName);
            _routingRegistrator.Handle(createHandlerRoute);
        }

        public IHandlerBuilder<TMessage, THandler> WithCorrelation(string name)
        {
            CorrelationPropertyName = name;
            return this;
        }
    }
}