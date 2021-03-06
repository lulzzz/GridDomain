using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using GridDomain.Common;
using GridDomain.CQRS;

namespace GridDomain.Node.AkkaMessaging.Waiting
{
    /// <summary>
    ///     Create expectation condition
    ///     Each opertion is placed in brackets after definition.
    ///     E.G.
    ///     ExpectBuilder.And(A).Or(B).And(C) converts into ((A or B) and C)
    ///     and
    ///     ExpectBuilder.And(A).And(B).Or(C).And(D) into (((A and B) or C) and D)
    /// </summary>
    /// <typeparam name="T">type for return on Create method to better chaining</typeparam>
    /// <returns></returns>
    public class ConditionBuilder<T> : IConditionBuilder<T>
    {
        private Expression<Func<IEnumerable<object>, bool>> StopExpression { get; set; } = c => true;
        public Func<IEnumerable<object>, bool> StopCondition { get; private set; }

        //message filter should be able to proceed message with type from filter key
        public readonly IDictionary<Type, List<Func<object, bool>>> MessageFilters = new Dictionary<Type, List<Func<object, bool>>>();
        internal Func<TimeSpan?, T> CreateResultFunc;

        public T Create()
        {
            return Create(null);
        }

        protected T Create(TimeSpan? timeout)
        {
            return CreateResultFunc.Invoke(timeout);
        }

        public ConditionBuilder(Func<TimeSpan?, T> createResultFunc = null)
        {
            CreateResultFunc = createResultFunc;
        }

        public IConditionBuilder<T> And<TMsg>(Predicate<TMsg> filter = null) where TMsg : class
        {
            return filter == null
                       ? And(typeof(TMsg), DefaultFilter<TMsg>)
                       : And(typeof(TMsg), o => FilterDecorator(o, filter));
        }

        public IConditionBuilder<T> Or<TMsg>(Predicate<TMsg> filter = null) where TMsg : class
        {
            return filter == null
                       ? Or(typeof(TMsg), DefaultFilter<TMsg>)
                       : Or(typeof(TMsg), o => FilterDecorator(o, filter));
        }

        public IConditionBuilder<T> And(Type type, Func<object, bool> filter = null)
        {
            var messageFilter = filter ?? DefaultFilter<object>;
            StopExpression = StopExpression.And(c => c != null && c.Any(messageFilter));
            StopCondition = StopExpression.Compile();

            AddFilter(type, messageFilter);
            return this;
        }

        protected IConditionBuilder<T> Or(Type type, Func<object, bool> filter = null)
        {
            var messageFilter = filter ?? DefaultFilter<object>;
            StopExpression = StopExpression.Or(c => c != null && c.Any(messageFilter));
            StopCondition = StopExpression.Compile();

            AddFilter(type, messageFilter);
            return this;
        }

        protected virtual bool DefaultFilter<TMsg>(object message)
        {
            return message is TMsg;
        }

        protected virtual bool FilterDecorator<TMsg>(object receivedMessage, Predicate<TMsg> domainMessageFilter) where TMsg : class
        {
            return receivedMessage is TMsg msg && domainMessageFilter(msg);
        }

        protected virtual void AddFilter(Type type, Func<object, bool> filter)
        {
            Condition.NotNull(() => filter);

            if (!MessageFilters.TryGetValue(type, out var list))
                list = MessageFilters[type] = new List<Func<object, bool>>();

            list.Add(filter);
        }
    }
}