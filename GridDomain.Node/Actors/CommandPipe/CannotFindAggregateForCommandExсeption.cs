using System;

namespace GridDomain.Node.Actors.CommandPipe
{
    public class CannotFindAggregateForCommandEx˝eption : Exception
    {
        public object Command { get; }
        public Type Topic { get; }

        public CannotFindAggregateForCommandEx˝eption(object command, Type topic)
        {
            Command = command;
            Topic = topic;
        }
    }
}