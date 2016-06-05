using System;
using Akka.Actor;
using Akka.DI.Core;
using Akka.DI.Unity;
using Akka.Serialization;
using Microsoft.Practices.Unity;

namespace GridDomain.Node.AkkaMessaging.Routing
{
    public class UnityDependencyResolverSerializer : Serializer
    {
        //HACK: must be set externally 8( 
        public static IUnityContainer Container;
        public UnityDependencyResolverSerializer(ExtendedActorSystem system) : base(system)
        {
        }

        public override byte[] ToBinary(object obj)
        {
            return GetBytes("UnityDependencyResolver");
        }
        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public override int Identifier => 1988;
        public override bool IncludeManifest => false;

        public override object FromBinary(byte[] bytes, Type type)
        {
  //         #if (type != typeof (UnityDependencyResolver))
// throw new ArgumentException(nameof(type));

            return new UnityDependencyResolver(Container, this.system);
        }
    }
}