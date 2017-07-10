namespace GridDomain.Node.Actors.Aggregates.Messages {
    public class CommandHandlerFinished
    {
        private CommandHandlerFinished()
        {
            
        }
        public static CommandHandlerFinished Instance { get; } = new CommandHandlerFinished();
    }
}