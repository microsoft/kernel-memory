// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline.Queue;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Microsoft.KernelMemory.Orchestration.RabbitMQ;

[Experimental("KMEXP04")]
public sealed class RabbitMQPipeline : IQueue
{
    private readonly ILogger<RabbitMQPipeline> _log;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly AsyncEventingBasicConsumer _consumer;
    private readonly RabbitMQConfig _config;
    private readonly int _messageTTLMsecs;
    private readonly int _delayBeforeRetryingMsecs;
    private readonly int _maxAttempts;
    private string _queueName = string.Empty;
    private string _poisonQueueName = string.Empty;

    /// <summary>
    /// Create a new RabbitMQ queue instance
    /// </summary>
    public RabbitMQPipeline(RabbitMQConfig config, ILoggerFactory? loggerFactory = null)
    {
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<RabbitMQPipeline>();

        this._config = config;
        this._config.Validate(this._log);

        // see https://www.rabbitmq.com/dotnet-api-guide.html#consuming-async
        var factory = new ConnectionFactory
        {
            ClientProvidedName = "KernelMemory",
            HostName = config.Host,
            Port = config.Port,
            UserName = config.Username,
            Password = config.Password,
            VirtualHost = !string.IsNullOrWhiteSpace(config.VirtualHost) ? config.VirtualHost : "/",
            DispatchConsumersAsync = true,
            ConsumerDispatchConcurrency = config.ConcurrentThreads,
            Ssl = new SslOption
            {
                Enabled = config.SslEnabled,
                ServerName = config.Host,
            }
        };

        this._messageTTLMsecs = config.MessageTTLSecs * 1000;
        this._connection = factory.CreateConnection();
        this._channel = this._connection.CreateModel();
        this._channel.BasicQos(prefetchSize: 0, prefetchCount: config.PrefetchCount, global: false);
        this._consumer = new AsyncEventingBasicConsumer(this._channel);

        this._delayBeforeRetryingMsecs = Math.Max(0, this._config.DelayBeforeRetryingMsecs);
        this._maxAttempts = Math.Max(0, this._config.MaxRetriesBeforePoisonQueue) + 1;
    }

    /// <inheritdoc />
    /// About dead letters, see https://www.rabbitmq.com/docs/dlx
    public Task<IQueue> ConnectToQueueAsync(string queueName, QueueOptions options = default, CancellationToken cancellationToken = default)
    {
        ArgumentNullExceptionEx.ThrowIfNullOrWhiteSpace(queueName, nameof(queueName), "The queue name is empty");
        ArgumentExceptionEx.ThrowIf(queueName.StartsWith("amq.", StringComparison.OrdinalIgnoreCase), nameof(queueName), "The queue name cannot start with 'amq.'");

        var poisonExchangeName = $"{queueName}.dlx";
        var poisonQueueName = $"{queueName}{this._config.PoisonQueueSuffix}";

        ArgumentExceptionEx.ThrowIf((Encoding.UTF8.GetByteCount(queueName) > 255), nameof(queueName),
            $"The queue name '{queueName}' is too long, max 255 UTF8 bytes allowed");
        ArgumentExceptionEx.ThrowIf((Encoding.UTF8.GetByteCount(poisonExchangeName) > 255), nameof(poisonExchangeName),
            $"The exchange name '{poisonExchangeName}' is too long, max 255 UTF8 bytes allowed, try using a shorter queue name");
        ArgumentExceptionEx.ThrowIf((Encoding.UTF8.GetByteCount(poisonQueueName) > 255), nameof(poisonQueueName),
            $"The dead letter queue name '{poisonQueueName}' is too long, max 255 UTF8 bytes allowed, try using a shorter queue name");

        if (!string.IsNullOrEmpty(this._queueName))
        {
            throw new InvalidOperationException($"The client is already connected to `{this._queueName}`");
        }

        // Define queue where messages are sent by the orchestrator
        this._queueName = queueName;
        try
        {
            this._channel.QueueDeclare(
                queue: this._queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    ["x-queue-type"] = "quorum",
                    ["x-delivery-limit"] = this._config.MaxRetriesBeforePoisonQueue,
                    ["x-dead-letter-exchange"] = poisonExchangeName
                });
            this._log.LogTrace("Queue name: {0}", this._queueName);
        }
#pragma warning disable CA2254
        catch (OperationInterruptedException ex)
        {
            var err = ex.Message;
            if (ex.Message.Contains("inequivalent arg 'x-delivery-limit'", StringComparison.OrdinalIgnoreCase))
            {
                err = $"The queue '{this._queueName}' is already configured with a different value for 'x-delivery-limit' " +
                      $"({nameof(this._config.MaxRetriesBeforePoisonQueue)}), the value cannot be changed to {this._config.MaxRetriesBeforePoisonQueue}";
            }
            else if (ex.Message.Contains("inequivalent arg 'x-dead-letter-exchange'", StringComparison.OrdinalIgnoreCase))
            {
                err = $"The queue '{this._queueName}' is already linked to a different dead letter exchange, " +
                      $"it is not possible to change the 'x-dead-letter-exchange' value to {poisonExchangeName}";
            }

            this._log.LogError(ex, err);
            throw new KernelMemoryException(err, ex);
        }
#pragma warning restore CA2254

        // Define poison queue where failed messages are stored
        this._poisonQueueName = poisonQueueName;
        this._channel.QueueDeclare(
            queue: this._poisonQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // Define exchange to route failed messages to poison queue
        this._channel.ExchangeDeclare(poisonExchangeName, "fanout", durable: true, autoDelete: false);
        this._channel.QueueBind(this._poisonQueueName, poisonExchangeName, routingKey: string.Empty, arguments: null);
        this._log.LogTrace("Poison queue name '{0}' bound to exchange '{1}' for queue '{2}'", this._poisonQueueName, poisonExchangeName, this._queueName);

        // Activate consumer
        if (options.DequeueEnabled)
        {
            this._channel.BasicConsume(queue: this._queueName, autoAck: false, consumer: this._consumer);
            this._log.LogTrace("Enabling dequeue on queue `{0}`", this._queueName);
        }

        return Task.FromResult<IQueue>(this);
    }

    /// <inheritdoc />
    public Task EnqueueAsync(string message, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (string.IsNullOrEmpty(this._queueName))
        {
            throw new InvalidOperationException("The client must be connected to a queue first");
        }

        this.PublishMessage(
            queueName: this._queueName,
            body: Encoding.UTF8.GetBytes(message),
            messageId: Guid.NewGuid().ToString("N"),
            expirationMsecs: this._messageTTLMsecs);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void OnDequeue(Func<string, Task<bool>> processMessageAction)
    {
        this._consumer.Received += async (object sender, BasicDeliverEventArgs args) =>
        {
            // Just for logging, extract the attempt number from the message headers
            var attemptNumber = 1;
            if (args.BasicProperties?.Headers != null && args.BasicProperties.Headers.TryGetValue("x-delivery-count", out object? value))
            {
                attemptNumber = int.TryParse(value.ToString(), out var parsedResult) ? ++parsedResult : -1;
            }

            try
            {
                this._log.LogDebug("Message '{0}' received, expires after {1}ms, attempt {2} of {3}",
                    args.BasicProperties?.MessageId, args.BasicProperties?.Expiration, attemptNumber, this._maxAttempts);

                byte[] body = args.Body.ToArray();
                string message = Encoding.UTF8.GetString(body);

                bool success = await processMessageAction.Invoke(message).ConfigureAwait(false);
                if (success)
                {
                    this._log.LogTrace("Message '{0}' successfully processed, deleting message", args.BasicProperties?.MessageId);
                    this._channel.BasicAck(args.DeliveryTag, multiple: false);
                }
                else
                {
                    if (attemptNumber < this._maxAttempts)
                    {
                        this._log.LogWarning("Message '{0}' failed to process (attempt {1} of {2}), putting message back in the queue",
                            args.BasicProperties?.MessageId, attemptNumber, this._maxAttempts);
                        if (this._delayBeforeRetryingMsecs > 0)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(this._delayBeforeRetryingMsecs)).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        this._log.LogError("Message '{0}' failed to process (attempt {1} of {2}), moving message to dead letter queue",
                            args.BasicProperties?.MessageId, attemptNumber, this._maxAttempts);
                    }

                    // Note: if "requeue == false" the message would be moved to the dead letter exchange
                    this._channel.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
                }
            }
#pragma warning disable CA1031 // Must catch all to handle queue properly
            catch (Exception e)
            {
                // Exceptions caught by this block:
                // - message processing failed with exception
                // - failed to delete message from queue
                // - failed to unlock message in the queue

                if (attemptNumber < this._maxAttempts)
                {
                    this._log.LogWarning(e, "Message '{0}' processing failed with exception (attempt {1} of {2}), putting message back in the queue",
                        args.BasicProperties?.MessageId, attemptNumber, this._maxAttempts);
                    if (this._delayBeforeRetryingMsecs > 0)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(this._delayBeforeRetryingMsecs)).ConfigureAwait(false);
                    }
                }
                else
                {
                    this._log.LogError(e, "Message '{0}' processing failed with exception (attempt {1} of {2}), putting message back in the queue",
                        args.BasicProperties?.MessageId, attemptNumber, this._maxAttempts);
                }

                // TODO: verify and document what happens if this fails. RabbitMQ should automatically unlock messages.
                // Note: if "requeue == false" the message would be moved to the dead letter exchange
                this._channel.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
            }
#pragma warning restore CA1031
        };
    }

    public void Dispose()
    {
        this._channel.Close();
        this._connection.Close();

        this._channel.Dispose();
        this._connection.Dispose();
    }

    private void PublishMessage(
        string queueName,
        ReadOnlyMemory<byte> body,
        string messageId,
        int? expirationMsecs)
    {
        var properties = this._channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = messageId;

        if (expirationMsecs.HasValue)
        {
            properties.Expiration = $"{expirationMsecs}";
        }

        this._log.LogDebug("Sending message to {0}: {1} (TTL: {2} secs)...",
            queueName, properties.MessageId, expirationMsecs.HasValue ? expirationMsecs / 1000 : "infinite");

        this._channel.BasicPublish(
            routingKey: queueName,
            body: body,
            exchange: string.Empty,
            basicProperties: properties);

        this._log.LogDebug("Message sent: {0} (TTL: {1} secs)", properties.MessageId, expirationMsecs.HasValue ? expirationMsecs / 1000 : "infinite");
    }
}
