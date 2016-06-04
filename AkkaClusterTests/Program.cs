

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Akka.Actor;
using Akka.Cluster.Routing;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Configuration;
using Akka.Configuration.Hocon;
using Akka.DI.Core;
using Akka.DI.Unity;
using Akka.Event;
using Akka.Routing;
using Akka.Util.Internal;
using Microsoft.Practices.Unity;
using NLog;
using NUnit.Framework;
using Samples.Cluster.Simple.Samples.Cluster.Simple;

namespace Samples.Cluster.Simple
{

    class EchoActor : UntypedActor
    {
        public EchoActor(string key)
        {
            _key = key;
            _privateKey = Guid.NewGuid();
        }
        protected ILoggingAdapter Log = Context.GetLogger();
        //public EchoActor()
        //{
        //    _privateKey = Guid.NewGuid();
        //}

        private readonly string _key = "default";
        private readonly Guid _privateKey;

        protected override void OnReceive(object message)
        {
            Log.Warning($"Echo from  {_privateKey} injected:{_key} payload: {message}");
        }
    }


    class Echo
    {
        public string Payload;
        public Guid Id;
    }

    class Program
    {
        public static void Main(string[] args)
        {
            StartUp();
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        public static void StartUp()
        {
            var ports = new[] {"2551", "2552", "0", "0", "0"};
            var section = (AkkaConfigurationSection) ConfigurationManager.GetSection("akka");
            var container = new UnityContainer();
            container.RegisterInstance("key1");
            var systems = new List<ActorSystem>();

            foreach (var port in ports)
            {
                //Override the configuration of the port
                var config =
                    ConfigurationFactory.ParseString("akka.remote.helios.tcp.port=" + port)
                        .WithFallback(section.AkkaConfig);

                //create an Akka system
                var system = ActorSystem.Create("ClusterSystem", config);
                systems.Add(system);
                system.AddDependencyResolver(new UnityDependencyResolver(container, system));
                //create an actor that handles cluster domain events
                //system.ActorOf(system.DI().Props<SimpleClusterListener>());
            }

            var actorSystem = systems.Last();


            var localPool =
                new ConsistentHashingPool(Environment.ProcessorCount)
                    .WithHashMapping(m => ((Echo)m).Id);

            var router = new ClusterRouterPool(localPool, new ClusterRouterPoolSettings(10, true, 2));

            var actorProps = actorSystem.DI().Props<EchoActor>();

            var pooledActorProps = actorProps.WithRouter(router);

            var pooledActor = actorSystem.ActorOf(pooledActorProps);


            var transport = DistributedPubSub.Get(actorSystem).Mediator;

            transport.Ask(new Subscribe("testTopic",pooledActor));

            for (int i = 0; i < 100; i++)
                transport.Tell(new Publish("testTopic", 
                                           new Echo() {Id = Guid.NewGuid(), Payload = "test load " + i}
                                           ));

            Thread.Sleep(10000);

            Akka.Cluster.Cluster.Get(actorSystem).Shutdown();
        }
    }

}