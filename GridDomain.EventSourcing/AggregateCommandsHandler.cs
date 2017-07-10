using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using GridDomain.Common;
using GridDomain.CQRS;

namespace GridDomain.EventSourcing
{

    public delegate Task<TAggregate> CommandExecutionDelegate<TAggregate>(TAggregate agr, ICommand cmd, PersistenceDelegate persistenceDelegate) where TAggregate : Aggregate;

    public class AggregateCommandsHandler<TAggregate> : TypeCatalog<CommandExecutionDelegate<TAggregate>, ICommand>,
                                                        IAggregateCommandsHandler<TAggregate> where TAggregate : Aggregate
                                                      
    {
        public IReadOnlyCollection<Type> RegisteredCommands => Catalog.Keys.ToArray();
        public Type AggregateType { get; } = typeof(TAggregate);

        public Task ExecuteAsync(TAggregate aggregate, ICommand command, PersistenceDelegate persistenceDelegate)
        {
            return Get(command).Invoke(aggregate, command, persistenceDelegate);
        }

        public override CommandExecutionDelegate<TAggregate> Get(ICommand command)
        {
            var handler = base.Get(command);
            if (handler == null)
                throw new CannotFindAggregateCommandHandlerExeption(typeof(TAggregate), command.GetType());
            return handler;
        }

        protected void Map<TCommand>(Func<TCommand, TAggregate, Task> commandExecutor) where TCommand : ICommand
        {
            Add<TCommand>( (a, c, p) =>
                          {
                              return commandExecutor((TCommand) c, a)
                                            .ContinueWith(t => p(a), TaskContinuationOptions.OnlyOnRanToCompletion)
                                            .ContinueWith(t => a);
                              
                          });
        }

        public void Map<TCommand>(Action<TCommand, TAggregate> commandExecutor) where TCommand : ICommand
        {
            Add<TCommand>((a, c, p) =>  {
                               try
                               {
                                   commandExecutor((TCommand) c, a);

                                   return p(a).ContinueWith(t => a);
                               }
                               catch(Exception ex)
                               {
                                   return Task.FromException<TAggregate>(ex);
                               }
                           });
        }

        
        protected void Map<TCommand>(Func<TCommand, TAggregate> commandExecutor) where TCommand : ICommand
        {
            Add<TCommand>((a, c, p) =>
                          {
                              try
                              {
                                  var agr = commandExecutor((TCommand)c);
                                  agr.Persist = p;
                                  return p(agr).ContinueWith(t => agr);
                              }
                              catch(Exception ex)
                              {
                                  return Task.FromException<TAggregate>(ex);
                              }
                          });
        }
    }
}