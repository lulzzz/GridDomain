﻿using System;
using System.Linq;
using System.Reflection;


using GridDomain.EventSourcing.CommonDomain;

namespace GridDomain.EventSourcing
{
    // By convention, I want to provide two means for creating domain objects. To the public, I want
    // to provide an always-valid constructor. This explicitly shows what needs to be provided to the domain
    // to create a valid instance of that object (eg, Person needs a twitter handle to be valid if I were doing twitter stream analysis)
    // Internally, to EventStore, I want it to be able to create my object via a private ctor and I'm going to pass in the
    // objects id.
    public class AggregateFactory : IConstructAggregates, IConstructSnapshots
    {
        //default convention: Aggregate is implementing IMemento itself
        protected virtual IAggregate BuildFromSnapshot(Type type, string id, IMemento snapshot)
        {
            var snapshotVersion = snapshot.Version;
            if (!(snapshot is IAggregate aggregate))
                throw new InvalidDefaultMementoException(type, id, snapshot);

            ((IMemento)aggregate).Version = snapshotVersion;
            aggregate.ClearUncommitedEvents();
            return aggregate;
        }

        protected static IAggregate BuildByConvention(Type type, string id)
        {
            //TODO: add type cache to reduce search time
            var constructor = type.GetTypeInfo()
                                  .DeclaredConstructors.FirstOrDefault(c =>
                                                              {
                                                                  var parameters = c.GetParameters();
                                                                  return parameters.Length == 1 && parameters[0]
                                                                             .ParameterType == typeof(string);
                                                              });

            if (constructor == null)
                throw new ConventionBasedConstructorNotFound();

            var aggregate = (IAggregate)constructor.Invoke(new object[] {id});
            aggregate.ClearUncommitedEvents();
            return aggregate;
        }

        public virtual IAggregate Build(Type type, string id, IMemento snapshot=null)
        {
            return snapshot == null ? BuildByConvention(type, id) : BuildFromSnapshot(type, id, snapshot);
        }

        public static readonly AggregateFactory Default = new AggregateFactory();

        public static T BuildEmpty<T>(string id = null) where T : IAggregate
        {
            return Default.Build<T>(id ?? Guid.NewGuid().ToString());
        }

        public virtual IMemento GetSnapshot(IAggregate aggregate)
        {
            return (Aggregate)aggregate;
        }
    }
}