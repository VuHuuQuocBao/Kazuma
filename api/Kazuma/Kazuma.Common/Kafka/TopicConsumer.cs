using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kazuma.Common.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kazuma.Common.Kafka
{
    public interface ITopicConsumer : IDisposable
    {
        void Start(KafkaConsumerConfiguration kafkaConsumerConfiguration);
        void Stop();
        Task Execute(CancellationToken cancellationToken);
        bool Disabled { get; }
    }
    public abstract class TopicConsumer : ITopicConsumer
    {
        protected readonly ILogger Logger;
        protected readonly string Topic;

        protected TopicConsumer(ILogger logger, string topic)
        {
            this.Logger = logger;
            this.Topic = topic;
        }

        public abstract void Dispose();

        public virtual bool Disabled { get => false; }

        public abstract void Start(KafkaConsumerConfiguration kafkaConsumerConfiguration);
        public abstract void Stop();
        public abstract Task Execute(CancellationToken cancellationToken);
    }

    public abstract class TopicConsumerBase<TKey, TValue> : TopicConsumer
    {
        /// <summary>
        /// Maximum of message should be pre-consumed into local Buffer Queue (fix OOM for large Kafka queue)
        /// 10K x 4K/large-message ~ 40MB buffer/topic
        /// </summary>
        private const int _MaxBufferMessages = 10000;
        protected internal IConsumer<TKey, TValue> _consumer;
        protected internal ConcurrentQueue<ConsumeResult<TKey, TValue>> _queue = new ConcurrentQueue<ConsumeResult<TKey, TValue>>();
        private SemaphoreSlim _signal = new SemaphoreSlim(0);

        protected TopicConsumerBase(ILogger logger, string topic)
            : base(logger, topic)
        {
        }

        protected virtual int MaxBufferMessages { get => _MaxBufferMessages; }

        public override void Dispose()
        {
            if (_consumer != null)
            {
                _consumer.Dispose();
                _consumer = null;

                _signal.Dispose();
            }
        }
        // subscribe
        public override void Start(KafkaConsumerConfiguration kafkaConsumerConfiguration)
        {
            var consumerBuilder = new ConsumerBuilder<TKey, TValue>(kafkaConsumerConfiguration);
            /*            if (BaseStartup.IsWorker)
                        {
                            consumerBuilder
                            .SetErrorHandler((_, error) =>
                            {
                                if (!Env.IsLocal)
                                    Environment.FailFast("Error - TERMINATE! " + error);
                                else
                                    Console.WriteLine(error);
                            })
                            .SetLogHandler((_, log) =>
                            {
                                if (!Env.IsLocal && log.Level is SyslogLevel.Error)
                                {
                                    Environment.FailFast("Error - TERMINATE! " + log);
                                }
                                else
                                    Console.WriteLine(log?.Message);
                            });
                        }*/
            //consumerBuilder.SetKeyDeserializer(Json.GetDeserializer<TKey>());
            //consumerBuilder.SetValueDeserializer(Json.GetDeserializer<TValue>());

            consumerBuilder.SetKeyDeserializer(new JsonDeserializer<TKey>());
            consumerBuilder.SetValueDeserializer(new JsonDeserializer<TValue>());

            _consumer = consumerBuilder.Build();
            Logger.LogInformation("{@consumer}[{@topic}] start consuming messages", this.GetType().Name, Topic);
            _consumer.Subscribe(Topic);
        }

        public override void Stop()
        {
            if (_consumer != null)
            {
                _consumer.Close();
                _consumer = null;
                Logger.LogInformation("{@consumer}[{@topic}] stop consuming messages", this.GetType().Name, Topic);
            }
        }

        public override async Task Execute(CancellationToken cancellationToken)
        {
            // consume and enqueue to queue, release the semaphore
            Console.WriteLine("Start To Consume Somebullshit");
            using var task = Task.Run(() => Consume(cancellationToken), cancellationToken);

            for (; _consumer != null && !cancellationToken.IsCancellationRequested;
                await _signal.WaitAsync(cancellationToken)) // wait for relase in consume
            {
                while (_queue.TryPeek(out var consumeResult))
                {
                    await Process(consumeResult, cancellationToken);
                }
            }
            // process xong release
            _signal.Release();
            task.Wait(cancellationToken);
        }

        private async Task Consume(CancellationToken cancellationToken)
        {
            for (ConsumeResult<TKey, TValue> consumeResult = null;
                _consumer != null && !cancellationToken.IsCancellationRequested;
                consumeResult = null)
            {
                // too much message, wait for 1s to process message 
                if (_queue.Count >= MaxBufferMessages)
                {
                    await Task.Delay(1000);
                }
                else
                {
                    try
                    {
                        // consume message from topic
                        consumeResult = _consumer.Consume(1000);
                        if (consumeResult != null && !consumeResult.IsPartitionEOF &&
                            _consumer != null && !cancellationToken.IsCancellationRequested)
                        {
                            // enqueue
                            _queue.Enqueue(consumeResult);
                            _signal.Release();
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        Logger.LogError(ex, "{@consumer}[{@topic}] consume message failed, will retry", this.GetType().Name, Topic);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "{@consumer}[{@topic}] unexpected error while consuming message: {@message}", this.GetType().Name, Topic/*, Json.Serialize(consumeResult)*/);
                    }
                }
            }
        }

        protected internal abstract Task Process(ConsumeResult<TKey, TValue> consumeResult, CancellationToken cancellationToken);
    }

    public abstract class TopicConsumer<TKey, TValue> : TopicConsumerBase<TKey, TValue>
    {
        protected TopicConsumer(ILogger logger, string topic)
            : base(logger, topic)
        {
        }

        protected internal override async Task Process(ConsumeResult<TKey, TValue> consumeResult, CancellationToken cancellationToken)
        {
            try
            {
                await OnMessageAsync(consumeResult, cancellationToken);
                if (_consumer != null && !cancellationToken.IsCancellationRequested)
                {
                    _consumer?.Commit(consumeResult);
                    _queue.TryDequeue(out _);
                }
            }
          /*  catch (TopicConsumeException ex)
            {
                if (ex.ByPass && _consumer != null && !cancellationToken.IsCancellationRequested)
                {
                    _consumer?.Commit(consumeResult);
                    _queue.TryDequeue(out _);
                }
            }*/
            catch (Exception ex)
            {
                Logger.LogError(ex, "{@consumer}[{@topic}] unexpected error while processing message: {@message}", this.GetType().Name, Topic/*, Json.Serialize(consumeResult)*/);
                await Task.Delay(5000); // delay 5s before processing next message
            }
        }

        protected abstract Task OnMessageAsync(ConsumeResult<TKey, TValue> consumeResult, CancellationToken stoppingToken);
    }

   /* public abstract class EntityConsumer<TContext, TKey, TValue> : TopicConsumer<TKey, TValue>
        where TContext : DbContextBase
    {
        protected readonly IServiceProvider _services;

        protected EntityConsumer(ILogger logger, IServiceProvider services, string topic)
            : base(logger, topic)
        {
            _services = services;
        }

        protected sealed override async Task OnMessageAsync(ConsumeResult<TKey, TValue> consumeResult, CancellationToken stoppingToken)
        {
            using (var scope = _services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TContext>();

                await this.OnMessageAsync(context, consumeResult, stoppingToken);
            }
        }

        protected abstract Task OnMessageAsync(TContext context, ConsumeResult<TKey, TValue> consumeResult, CancellationToken stoppingToken);
    }*/

    public abstract class BatchTopicConsumer<TKey, TValue> : TopicConsumerBase<TKey, TValue>
    {
        protected BatchTopicConsumer(ILogger logger, string topic)
            : base(logger, topic)
        {
        }

        protected internal override async Task Process(ConsumeResult<TKey, TValue> consumeResult, CancellationToken cancellationToken)
        {
            // Try to take all messages in the queue
            var consumeResults = _queue.ToArray();
            try
            {
                // Process in batch
                await OnMessageAsync(consumeResults, cancellationToken);

                // Commit the last one & dequeue all
                if (_consumer != null && !cancellationToken.IsCancellationRequested)
                {
                    _consumer?.Commit(consumeResults[consumeResults.Length - 1]);
                    for (var i = consumeResults.Length - 1; i >= 0; i--)
                        _queue.TryDequeue(out _);
                }
            }
         /*   catch (TopicConsumeException ex)
            {
                // Commit the last one & dequeue all
                if (ex.ByPass && _consumer != null && !cancellationToken.IsCancellationRequested)
                {
                    _consumer?.Commit(consumeResults[consumeResults.Length - 1]);
                    for (var i = consumeResults.Length - 1; i >= 0; i--)
                        _queue.TryDequeue(out _);
                }
            }*/
            catch (Exception ex)
            {
                Logger.LogError(ex, "{@consumer}[{@topic}] unexpected error while processing message: {@message}", this.GetType().Name, Topic/*, Json.Serialize(consumeResult)*/);
                await Task.Delay(5000); // delay 5s before processing next message
            }
        }

        protected abstract Task OnMessageAsync(ConsumeResult<TKey, TValue>[] consumeResults, CancellationToken stoppingToken);
    }
/*
    public abstract class BatchEntityConsumer<TContext, TKey, TValue> : BatchTopicConsumer<TKey, TValue>
        where TContext : DbContextBase
    {
        protected readonly IServiceProvider _services;

        protected BatchEntityConsumer(ILogger logger, IServiceProvider services, string topic)
            : base(logger, topic)
        {
            _services = services;
        }

        protected sealed override async Task OnMessageAsync(ConsumeResult<TKey, TValue>[] consumeResults, CancellationToken stoppingToken)
        {
            using (var scope = _services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TContext>();

                await this.OnMessageAsync(context, consumeResults, stoppingToken);
            }
        }

        protected abstract Task OnMessageAsync(TContext context, ConsumeResult<TKey, TValue>[] consumeResults, CancellationToken stoppingToken);
    }*/

    public abstract class DeadTopicConsumer<TKey, TValue> : TopicConsumer<TKey, JObject>
    {
        sealed class DeadMessage
        {
            public int? retry_times { get; set; }
            public string retry_error { get; set; }
            public long? retry_at { get; set; }
        }

        protected readonly KafkaProducer _kafkaProducer;
        protected readonly int _maxRetry;
        protected readonly int _retrySeconds;

        /// <param name="maxRetry">Max retry allowed (default to 5 times)</param>
        /// <param name="retrySeconds">Seconds between retries for the same message (to wait for the 3rd party availability). Default to 60s, 0 to no wait</param>
        protected DeadTopicConsumer(ILogger logger, string topic, KafkaProducer kafkaProducer, int maxRetry = 5, int retrySeconds = 60)
            : base(logger, topic)
        {
            _kafkaProducer = kafkaProducer;
            _maxRetry = maxRetry;
            _retrySeconds = retrySeconds;
        }

        protected sealed override async Task OnMessageAsync(ConsumeResult<TKey, JObject> consumeResult, CancellationToken stoppingToken)
        {
            var jMessage = consumeResult.Message.Value;

            // Need to wait until timeout
            if (_retrySeconds > 0)
            {
                var lastRetryTimeSpan = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(jMessage[nameof(DeadMessage.retry_at)]?.ToObject<long?>() ?? 0L);
                if (0 < lastRetryTimeSpan.TotalSeconds && lastRetryTimeSpan.TotalSeconds < _retrySeconds)
                    await Task.Delay(lastRetryTimeSpan);
            }

            // Retry the msg
            var message = jMessage.ToObject<TValue>();
            try
            {
                await OnMessageAsync(consumeResult, message, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Skip the CanceledException
                throw;
            }
            catch (Exception ex)
            {
                var retryTimes = jMessage[nameof(DeadMessage.retry_times)]?.ToObject<int?>() ?? 1;
                if (retryTimes >= _maxRetry)
                    await OnMessageDropped(ex, consumeResult, message, stoppingToken);
                else
                {
                    // Retry by pushing the msg to the queue again
                    jMessage[nameof(DeadMessage.retry_times)] = JToken.FromObject(retryTimes + 1);
                    jMessage[nameof(DeadMessage.retry_error)] = JToken.FromObject(ex.ToString());
                    jMessage[nameof(DeadMessage.retry_at)] = JToken.FromObject(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    await _kafkaProducer.ProduceAsync(Topic, jMessage, stoppingToken);
                }
            }
        }

        protected virtual Task OnMessageDropped(Exception error, ConsumeResult<TKey, JObject> consumeResult, TValue message, CancellationToken stoppingToken)
        {
            //Logger.LogError(error, "{@consumer}[{@topic}] drop dead queue message after {@maxRetry} (retries): {@message}", this.GetType().Name, Topic, _maxRetry, Json.Serialize(consumeResult.Message.Value));
            return Task.CompletedTask;
        }

        protected abstract Task OnMessageAsync(ConsumeResult<TKey, JObject> consumeResult, TValue message, CancellationToken stoppingToken);
    }
    sealed class JsonDeserializer<T> : IDeserializer<T>
    {
        public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            return isNull || data == null || data.IsEmpty ? default(T) :
                JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data)/*, Json.Settings*/);
        }
    }

}
