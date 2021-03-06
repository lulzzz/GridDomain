using System;
using System.Threading.Tasks;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using Quartz;
using Serilog;

namespace GridDomain.Scheduling.Quartz
{
    public class QuartzJob : IJob
    {
        public const string CommandKey = nameof(CommandKey);
        public const string EventKey = nameof(EventKey);
        public const string ScheduleKey = nameof(ScheduleKey);
        public const string ExecutionOptionsKey = nameof(ExecutionOptionsKey);
        public const string MetadataKey = nameof(MetadataKey);
        public const string PassingCommandToExecutor = "passing command to executor";
        public const string CommandRaiseTime = "command raise time came";
        public const string PublishingEvent = "publishing event";
        public const string EventRaiseTimeCame = "event raise time came";
        public const string PublishingJobFailure = "publishing job faulire";
        public const string JobRaiseTimeCame = "job raise time came";
        private readonly DomainSerializer _serializer = new DomainSerializer();
        private readonly ProcessEntry _jobFailedProcessEntry = new ProcessEntry(nameof(QuartzJob), PublishingJobFailure, JobRaiseTimeCame);

        private readonly ICommandExecutor _executor;
        private readonly IPublisher _publisher;
        private readonly ILogger _quartzLogger;

        public QuartzJob(ILogger quartzLogger, IPublisher publisher, ICommandExecutor executor)
        {
            Condition.NotNull(() => quartzLogger);
            Condition.NotNull(() => publisher);

            _executor = executor;
            _publisher = publisher;
            _quartzLogger = quartzLogger.ForContext<QuartzJob>();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var jobDataMap = context.JobDetail.JobDataMap;
            IMessageMetadata metadata;
            try
            {
                metadata = Get<IMessageMetadata>(jobDataMap, MetadataKey);
            }
            catch
            {
                metadata = MessageMetadata.Empty;
            }

            var jobKey = context.JobDetail.Key;

            if (jobDataMap.ContainsKey(CommandKey))
            {
                var command = Get<Command>(jobDataMap, CommandKey);
                await WithErrorHandling(command, metadata, jobKey, () => ProcessCommand(command, jobDataMap, metadata, jobKey));
            }
            else
            {
                var evt = Get<DomainEvent>(jobDataMap, EventKey);
                await WithErrorHandling(evt, metadata, jobKey, () => ProcessEvent(metadata, jobKey, evt));
            }
        }

        private Task ProcessEvent(IMessageMetadata metadata, JobKey jobKey, DomainEvent messageToFire)
        {
            throw new NotImplementedException("need refactor - pass events to command pipe instead of just publish");
        }

        private async Task WithErrorHandling(IHaveId processingMessage, IMessageMetadata messageMetadata, JobKey key, Func<Task> act)
        {
            try
            {
               await act();
            }
            catch (Exception e)
            {
                var businessError = e.UnwrapSingle();
                _quartzLogger.Error(businessError, "job {key} failed", key.Name);
                var jobFailed = new JobFailed(key.Name, key.Group, businessError, processingMessage);
                var jobFailedMetadata = messageMetadata.CreateChild(processingMessage,
                                                                    _jobFailedProcessEntry);
                _publisher.Publish(jobFailed, jobFailedMetadata);
                throw new JobExecutionException(businessError, false);
            }
        }

        private async Task ProcessCommand(ICommand command, JobDataMap jobDataMap, IMessageMetadata metadata, JobKey jobKey)
        {
            var options = Get<ExecutionOptions>(jobDataMap, ExecutionOptionsKey);
            if (options.SuccesEventType == null)
                throw new OptionsNotFilledException("options do not have SuccessEventType for key " + jobKey);

            var commandMetadata = metadata.CreateChild(command.Id,
                                                       new ProcessEntry(nameof(QuartzJob), PassingCommandToExecutor, CommandRaiseTime));

            //waiting domain event by correlation id
            await _executor.Prepare(command, commandMetadata)
                           .Expect(options.SuccesEventType)
                           .Execute(options.Timeout, true);

            _quartzLogger.Information("job {key} succeed", jobKey.Name);

            var successMetadata = commandMetadata.CreateChild(Guid.NewGuid().ToString(),
                                                              new ProcessEntry(nameof(QuartzJob),
                                                                               "Publishing success notification",
                                                                               "Job execution completed succesfully. Command executed and confirmed."));

            var jobSucceeded = new JobSucceeded(jobKey.Name, jobKey.Group, command);

            _publisher.Publish(jobSucceeded, successMetadata);
        }

        public static IJobDetail Create(ScheduleKey key,
                                        Command command,
                                        IMessageMetadata metadata,
                                        ExecutionOptions executionOptions)
        {
            var jobDataMap = new JobDataMap
                             {
                                 {CommandKey, Serialize(command)},
                                 {ScheduleKey, Serialize(key)},
                                 {ExecutionOptionsKey, Serialize(executionOptions)},
                                 {MetadataKey, Serialize(metadata)}
                             };

            return CreateJob(key, jobDataMap);
        }

        public static IJobDetail Create(ScheduleKey key, DomainEvent eventToSchedule, IMessageMetadata metadata)
        {
            var serializedEvent = Serialize(eventToSchedule);
            var serializedKey = Serialize(key);
            var serializedMetadata = Serialize(metadata);

            var jobDataMap = new JobDataMap
                             {
                                 {EventKey, serializedEvent},
                                 {ScheduleKey, serializedKey},
                                 {MetadataKey, serializedMetadata}
                             };

            return CreateJob(key, jobDataMap);
        }

        private static byte[] Serialize(object source, DomainSerializer serializer = null)
        {
            return (serializer ?? new DomainSerializer()).ToBinary(source);
        }

        private static T Deserialize<T>(byte[] source, DomainSerializer serializer = null)
        {
            return (T) (serializer ?? new DomainSerializer()).FromBinary(source, typeof(T));
        }

        private T Get<T>(JobDataMap map, string key)
        {
            var bytes = map[key] as byte[];
            return Deserialize<T>(bytes, _serializer);
        }

        public static IJobDetail CreateJob(ScheduleKey key, JobDataMap jobDataMap)
        {
            return JobBuilder.Create<QuartzJob>()
                             .WithIdentity(key.ToJobKey())
                             .WithDescription(key.Description)
                             .UsingJobData(jobDataMap)
                             .RequestRecovery(true)
                             .Build();
        }
    }
}