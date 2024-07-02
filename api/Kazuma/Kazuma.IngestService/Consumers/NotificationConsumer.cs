using Confluent.Kafka;
using Kazuma.Common.Kafka;

namespace Kazuma.IngestService.Consumers
{
    public class NotificationConsumer : TopicConsumer<string, KafkaMessage>
    {
        public NotificationConsumer(ILogger<NotificationConsumer> logger) : base(logger, "Test-Notification")
        {
        }

        protected override Task OnMessageAsync(ConsumeResult<string, KafkaMessage> consumeResult, CancellationToken stoppingToken)
        {
            var message = consumeResult.Message.Value;
            var a = 1;
            return Task.CompletedTask;
        }
    }
    public class KafkaMessage
    {
        public string message { get; set; }
    }
}


