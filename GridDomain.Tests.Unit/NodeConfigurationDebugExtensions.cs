using GridDomain.Node.Configuration;

namespace GridDomain.Tests.Unit {
    public static class NodeConfigurationDebugExtensions
    {
        public static string ToDebugStandAloneSystemConfig(this AkkaConfiguration conf)
        {
#if DEBUG
            return conf.ToStandAloneSystemConfig(true);
#else
            return conf.ToStandAloneSystemConfig(false);
#endif
        }

        public static string ToDebugStandAloneInMemorySystemConfig(this AkkaConfiguration conf)
        {
#if DEBUG
            return conf.ToStandAloneInMemorySystemConfig(true);
#else
            return conf.ToStandAloneInMemorySystemConfig(false);
#endif
        }
    }
}