using System;
using System.Linq;
using System.Threading;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.TestKit.NUnit;
using GridDomain.CQRS;
using GridDomain.Node;
using GridDomain.Node.Configuration;
using GridDomain.Tests.Acceptance.Persistence;
using Microsoft.Practices.Unity;
using NUnit.Framework;

namespace GridDomain.Tests.Acceptance
{
    public abstract class NodeCommandsTest : TestKit
    {
        protected abstract AkkaConfiguration AkkaConf { get; }
        private IActorSubscriber _subscriber;
        protected GridDomainNode GridNode;

        protected NodeCommandsTest(string config, string name = null) : base(config, name)
        {
        }

        protected abstract TimeSpan Timeout { get; }

        [TearDown]
        public void DeleteSystems()
        {
            Console.WriteLine();
            Console.WriteLine("Stopping node");
            GridNode.Stop();
        }

        [TestFixtureSetUp]
        protected void Init()
        {
            var autoTestGridDomainConfiguration = TestEnvironment.Configuration;

            TestDbTools.ClearData(autoTestGridDomainConfiguration, AkkaConf.Persistence);

            GridNode = GreateGridDomainNode(AkkaConf, autoTestGridDomainConfiguration);
            GridNode.Start(autoTestGridDomainConfiguration);
            _subscriber = GridNode.Container.Resolve<IActorSubscriber>();
        }

        protected abstract GridDomainNode GreateGridDomainNode(AkkaConfiguration akkaConf, IDbConfiguration dbConfig);

        protected void ExecuteAndWaitFor<TEvent>(ICommand[] commands, int eventNumber)
        {
            Console.WriteLine();
            Console.WriteLine($"Totally issued {commands.Length} {commands.First().GetType().Name}");
            Console.WriteLine();

            var actor = GridNode.System.ActorOf(Props.Create(() => new CountEventWaiter<TEvent>(eventNumber, TestActor)),
                "CountEventWaiter_for_"+typeof(TEvent).Name);

            _subscriber.Subscribe<TEvent>(actor);
            Console.WriteLine("Starting execute");

            foreach (var c in commands)
                GridNode.Execute(c);

            Console.WriteLine();
            Console.WriteLine($"Execution finished, wait started with timeout {Timeout}");

            ExpectMsg<ExpectedMessagesRecieved<TEvent>>(Timeout);

            Console.WriteLine();
            Console.WriteLine("Wait ended");
        }
    }
}