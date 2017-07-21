using GridDomain.Configuration;

using GridDomain.Node.Configuration.Composition;
using GridDomain.Node.Transports;
using GridDomain.Routing;
using Serilog;

namespace GridDomain.Node {
    public class MessageProcessContext : IMessageProcessContext
    {
        public MessageProcessContext(IActorTransport transport, ILogger log)
        {
            Publisher = transport;
            Log = log;
        }

        public IPublisher Publisher { get; }
        public ILogger Log { get; }
    }
}