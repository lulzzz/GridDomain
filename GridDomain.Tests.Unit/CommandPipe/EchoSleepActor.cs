using System;
using System.Threading.Tasks;
using Akka.Actor;
using GridDomain.Common;
using GridDomain.Node.Actors;

namespace GridDomain.Tests.Unit.CommandPipe
{
    internal class EchoSleepActor : ReceiveActor
    {
        public EchoSleepActor(TimeSpan sleepTime, IActorRef watcher)
        {
            Receive<IMessageMetadataEnvelop>(m => Task.Delay(sleepTime)
                                                      .ContinueWith(t => new MarkedHandlerExecutedMessage("123", m))
                                                      .PipeTo(Self,Sender));

            Receive<MarkedHandlerExecutedMessage>(m =>
                                                  {
                                                      watcher.Tell(m);
                                                      Sender.Tell(m);
                                                  });
        }
    }
}