using System;
using System.Collections.Generic;
using Akka.Actor;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.Node.Actors.EventSourced.Messages;

namespace GridDomain.Node.Actors.Aggregates {
    public class AggregateCommandExecutorActor : ReceiveActor
    {
        private readonly string _baseAggregatesPath;
        private readonly Dictionary<Guid,IActorRef> _knownAggregates = new Dictionary<Guid, IActorRef>();

        public AggregateCommandExecutorActor(string baseAggregatesPath, TimeSpan? lookupTimeout, TypeCatalog<Func<IActorRef>,object> aggregatesCreators)
        {
            var timeout = lookupTimeout ?? TimeSpan.FromSeconds(30);
            _baseAggregatesPath = baseAggregatesPath;
            Receive<IMessageMetadataEnvelop>(m =>
                                             {
                                                 var cmd = (ICommand)m.Message;
                                                 if (!_knownAggregates.TryGetValue(cmd.AggregateId, out var aggregateActor))
                                                 {
                                                     Context.ActorSelection(BuildPath(cmd.AggregateId))
                                                            .ResolveOne(timeout)
                                                            .ContinueWith(t =>
                                                                          {
                                                                              if (!t.IsFaulted)
                                                                                  return new PassToAggregate(m, t.Result, cmd.AggregateId);

                                                                              var aggregateRef = aggregatesCreators.Get(m.Message)();
                                                                              return new PassToAggregate(m, aggregateRef, cmd.AggregateId);
                                                                          })
                                                            .PipeTo(Self,Sender);
                                                 }
                                                 else
                                                 {
                                                     aggregateActor.Forward(m);
                                                 }

                                             }, m => m.Message is ICommand);
            Receive<PassToAggregate>(m =>
                                     {
                                         _knownAggregates[m.AggregateId] = m.AggregateRef;
                                         m.AggregateRef.Forward(m.Message);
                                         Context.WatchWith(m.AggregateRef, new AggregateTerminated(m.AggregateId));
                                     });

            Receive<AggregateTerminated>(m => _knownAggregates.Remove(m.AggregateId));
            Receive<HealthStatus>(m => { });
            Receive<AggregateWarmup>(cmd =>
                                     {
                                         var envelop = new MessageMetadataEnvelop(CheckHealth.Instance);
                                         Context.ActorSelection(BuildPath(cmd.AggregateId))
                                                .ResolveOne(timeout)
                                                .ContinueWith(t =>
                                                              {
                                                                  if (!t.IsFaulted)
                                                                      return new PassToAggregate(envelop, t.Result, cmd.AggregateId);

                                                                  var aggregateRef = aggregatesCreators.Get(cmd.AggregateType)();
                                                                  return new PassToAggregate(envelop, aggregateRef, cmd.AggregateId);
                                                              })
                                                .PipeTo(Self, Sender);
                                     },cmd => !_knownAggregates.ContainsKey(cmd.AggregateId));

        }

        class AggregateWarmup
        {
            public AggregateWarmup(Guid aggregateId, Type aggregateType)
            {
                AggregateId = aggregateId;
                AggregateType = aggregateType;
            }
            public Type AggregateType { get; }
            public Guid AggregateId { get; }
        }
        class AggregateTerminated
        {
            public AggregateTerminated(Guid aggregateId)
            {
                AggregateId = aggregateId;
            }
            public Guid AggregateId { get; }
        }
        class PassToAggregate
        {
            public Guid AggregateId { get; }
            public IActorRef AggregateRef { get; }
            public IMessageMetadataEnvelop Message { get; }
            public PassToAggregate(IMessageMetadataEnvelop message, IActorRef aggregateRef, Guid aggregateId)
            {
                AggregateId = aggregateId;
                Message = message;
                AggregateRef = aggregateRef;
            }
        }

 
        private string BuildPath(Guid cmdAggregateId)
        {
            return _baseAggregatesPath + "/" + cmdAggregateId;
        }
    }
}