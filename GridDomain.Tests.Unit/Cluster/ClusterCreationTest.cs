using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Cluster.TestKit;
using Akka.Cluster.Tests.MultiNode;
using Akka.Configuration;
using Akka.Remote;
using Akka.Remote.TestKit;
using Xunit;

namespace GridDomain.Tests.Unit.Cluster
{
    public class JoinInProgressMultiNodeConfig : MultiNodeConfig
    {
        public RoleName First { get; }
        public RoleName Second { get; }

        public JoinInProgressMultiNodeConfig()
        {
            First = Role("first");
            Second = Role("second");

            CommonConfig = MultiNodeLoggingConfig.LoggingConfig.WithFallback(DebugConfig(true))
                                                               .WithFallback(ConfigurationFactory.ParseString(@"
                                                                    akka.stdout-loglevel = DEBUG
                                                                    akka.cluster {
                                                                        # simulate delay in gossip by turning it off
                                                                        gossip-interval = 300 s
                                                                        failure-detector {
                                                                            threshold = 4
                                                                            acceptable-heartbeat-pause = 1 second
                                                                        }
                                                               }").WithFallback(MultiNodeClusterSpec.ClusterConfig()));

            NodeConfig(new []{ First },new []{ConfigurationFactory.ParseString("akka.cluster.roles = [frontend]")});
            NodeConfig(new []{ Second }, new []{ ConfigurationFactory.ParseString("akka.cluster.roles =[backend]")});
        }
    }

  
    public class JoinInProgressSpec : MultiNodeClusterSpec
    {
        readonly JoinInProgressMultiNodeConfig _config;

        public JoinInProgressSpec() : this(new JoinInProgressMultiNodeConfig())
        {
        }

        private JoinInProgressSpec(JoinInProgressMultiNodeConfig config) : base(config, typeof(JoinInProgressMultiNodeConfig))
        {
            _config = config;
        }

        [MultiNodeFact]
        public void AClusterNodeMustSendHeartbeatsImmediatelyWhenJoiningToAvoidFalseFailureDetectionDueToDelayedGossip()
        {
            RunOn(StartClusterNode, _config.First);

            EnterBarrier("first-started");

            RunOn(() => Cluster.Join(GetAddress(_config.First)), _config.Second);

            RunOn(() =>
                  {
                      var until = Deadline.Now + TimeSpan.FromSeconds(5);
                      while(!until.IsOverdue)
                      {
                          Thread.Sleep(200);
                          Assert.True(Cluster.FailureDetector.IsAvailable(GetAddress(_config.Second)));
                      }
                  }, _config.First);

            EnterBarrier("after");
        }
       
    }
}
