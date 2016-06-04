using System;
using Akka.DI.Core;
using Akka.DI.Unity;
using GridDomain.Balance.Node;
using GridDomain.Node;
using GridDomain.Node.Configuration;
using NUnit.Framework;

namespace GridDomain.Tests.Acceptance.Balance.ReadModelConcurrentBuild
{
    public class Cluster_Given_balance_change_plan_When_executing : Given_balance_change_plan_When_executing
    {
        private AkkaCluster _akkaCluster;
        private AutoTestAkkaConfiguration conf = new AutoTestAkkaConfiguration();
        protected override AkkaConfiguration AkkaConf => conf;

        protected override GridDomainNode GreateGridDomainNode(AkkaConfiguration akkaConf, IDbConfiguration dbConfig)
        {
            _akkaCluster = ActorSystemFactory.CreateCluster(AkkaConf);
            var unityContainer = CreateUnityContainer(dbConfig);

            return new GridDomainNode(unityContainer,
                                      new BalanceCommandsRouting(),
                                      TransportMode.Cluster, _akkaCluster.All);
        }

        /// <summary>
        ///     Important than persistence setting are the same as for testing cluster as for test ActorSystem
        /// </summary>
        public Cluster_Given_balance_change_plan_When_executing()
            : base(new AutoTestAkkaConfiguration().Copy("test", 9000)
                                                  .ToStandAloneSystemConfig())
        {

        }

        [TestFixtureTearDown]
        public void Terminate()
        {
            _akkaCluster.Dispose();
        }

        protected override TimeSpan Timeout => TimeSpan.FromSeconds(10);
        protected override int BusinessNum => 1;
        protected override int ChangesPerBusiness => 1;
    }
}