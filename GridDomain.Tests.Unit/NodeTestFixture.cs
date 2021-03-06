using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Akka.Actor;
using GridDomain.Common;
using GridDomain.Configuration;
using GridDomain.Node;
using GridDomain.Node.Configuration;
using GridDomain.Node.Configuration.Composition;
using GridDomain.Scheduling.Quartz;
using GridDomain.Scheduling.Quartz.Configuration;
using GridDomain.Tests.Common;
using GridDomain.Tests.Common.Configuration;
using Serilog;
using Serilog.Data;
using Serilog.Events;
using Xunit.Abstractions;

namespace GridDomain.Tests.Unit
{

    public class NodeTestFixture : IDisposable
    {
        public ITestOutputHelper Output { get; }

        private static readonly NodeConfiguration DefaultNodeConfig = new AutoTestNodeConfiguration();

        private readonly List<IDomainConfiguration> _domainConfigurations = new List<IDomainConfiguration>();

        public NodeTestFixture(ITestOutputHelper output, IDomainConfiguration domainConfiguration) : this(output, new[] {domainConfiguration})
        {
            
        }
       
        public NodeTestFixture(ITestOutputHelper output, 
                               IDomainConfiguration[] domainConfiguration = null, 
                               TimeSpan? defaultTimeout = null) :
            this(output,null,null,domainConfiguration,defaultTimeout)
                 {
                    
                 }
        
        public NodeTestFixture(ITestOutputHelper output, 
                               NodeConfiguration cfg, 
                               Func<NodeConfiguration, string> systemConfigFactorry=null, 
                               IDomainConfiguration[] domainConfiguration = null, 
                               TimeSpan? defaultTimeout = null)
        {
            Output = output;
            DefaultTimeout = defaultTimeout ?? DefaultTimeout;
            NodeConfig = cfg ?? DefaultNodeConfig;
            
            ConfigBuilder =  systemConfigFactorry ?? (n => n.ToStandAloneInMemorySystemConfig());
            SystemConfig = new Lazy<string>(() => ConfigBuilder(NodeConfig));
            if (domainConfiguration == null)
                return;
         
            foreach(var c in domainConfiguration)
                Add(c);
        }
        public GridDomainNode Node { get; private set; }
        public Func<NodeConfiguration,string> ConfigBuilder { get; set; }
        public Lazy<string> SystemConfig { get; }
        public NodeConfiguration NodeConfig { get; }
        public string Name => NodeConfig.Name;

      
        private const int DefaultTimeOutSec =
#if DEBUG
            10; //in debug mode all messages serialization is enabled, and it slows down all tests greatly
#endif
#if !DEBUG
            3;
#endif
        private TimeSpan DefaultTimeout { get; } = Debugger.IsAttached ? TimeSpan.FromHours(1) : TimeSpan.FromSeconds(DefaultTimeOutSec);

        public void Dispose()
        {
            Node.Stop().Wait();
        }

        public NodeTestFixture Add(IDomainConfiguration config)
        {
            _domainConfigurations.Add(config);
            return this;
        }
       
        private async Task<GridDomainNode> StartNode(GridDomainNode node)
        {
            OnNodePreparingEvent.Invoke(this, this);
            Node = node;
            Node.Initializing += (sender, n) => OnNodeCreatedEvent.Invoke(this, n);
            await Node.Start();
            OnNodeStartedEvent.Invoke(this, Node);

            return Node; 
        }

        public Task<GridDomainNode> CreateNode(ILogger logger=null)
        {
            return CreateNode(() => NodeConfig.CreateInMemorySystem(), logger ?? new XUnitAutoTestLoggerConfiguration(Output, NodeConfig.LogLevel).CreateLogger());
        }
            

        public Task<GridDomainNode> CreateNode(Func<ActorSystem> actorSystemProvider, ILogger logger)
        {
            return StartNode(new GridDomainNode(_domainConfigurations,
                                                new DelegateActorSystemFactory(actorSystemProvider), 
                                                logger, 
                                                DefaultTimeout));
        }

        public event EventHandler<GridDomainNode>  OnNodeStartedEvent   = delegate { };
        public event EventHandler<NodeTestFixture> OnNodePreparingEvent = delegate { };
        public event EventHandler<GridDomainNode>  OnNodeCreatedEvent   = delegate { };
    }
}