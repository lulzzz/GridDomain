using System.Collections.Specialized;
using GridDomain.Scheduling.Quartz.Retry;

namespace GridDomain.Scheduling.Quartz.Configuration
{
    public class InMemoryQuartzConfig : IQuartzConfig
    {
        private static int _number;

        public InMemoryQuartzConfig(IRetrySettings retrySettings =null, string schedulerName = null)
        {
            Name = schedulerName ?? "Scheduler_" + ++_number;
            RetryOptions = retrySettings ?? new InMemoryRetrySettings();
        }

        public NameValueCollection Settings
            =>
                new NameValueCollection
                {
                    ["quartz.jobStore.type"] = "Quartz.Simpl.RAMJobStore, Quartz",
                    ["quartz.scheduler.instanceName"] = Name
                };

        public string Name { get; }
        public IRetrySettings RetryOptions { get; set; }
    }
}