namespace GridDomain.Node.Configuration.Hocon {
    public class TransportConfig : IHoconConfig
    {
        private readonly bool _enforceIpVersion;
        private readonly string _host;
        private readonly int _port;
        private readonly string _publicHost;

        private TransportConfig(int port, string host, string publicHost, bool enforceIpVersion)
        {
            _enforceIpVersion = enforceIpVersion;
            _publicHost = publicHost;
            _host = host;
            _port = port;
        }

        public TransportConfig(INodeNetworkAddress config)
            : this(config.PortNumber, config.Host, config.PublicHost, config.EnforceIpVersion) {}

        public string Build()
        {
            var transportString = @"remote {
                    log-remote-lifecycle-events = DEBUG
                    dot-netty.tcp {
                               port = " + _port + @"
                               hostname =  " + _host + @"
                               public-hostname = " + _publicHost + @"
                    }
            }";
            return transportString;
        }
    }
}