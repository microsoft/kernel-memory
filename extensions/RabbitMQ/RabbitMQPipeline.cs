// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline.Queue;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microsoft.KernelMemory.Orchestration.RabbitMQ;

[Experimental("KMEXP04")]
public sealed class RabbitMQPipeline : IQueue
{
    private const string DeliveryCountHeader = "x-delivery-count";

    private readonly ILogger<RabbitMQPipeline> _log;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly AsyncEventingBasicConsumer _consumer;

    // Queue name
    private string _queueName = string.Empty;

    // Poison Queue name
    private string _poisonQueueName = string.Empty;

    private readonly int _messageTTLMsecs;

    // Queue confirguration
    private readonly RabbitMqConfig _config;

    /// <summary>
    /// Create a new RabbitMQ queue instance
    /// </summary>
    public RabbitMQPipeline(RabbitMqConfig config, ILogger<RabbitMQPipeline>? log = null)
    {
        this._config = config;
        this._config.Validate();

        this._log = log ?? DefaultLogger<RabbitMQPipeline>.Instance;

        // see https://www.rabbitmq.com/dotnet-api-guide.html#consuming-async
        var factory = new ConnectionFactory
        {
            HostName = config.Host,
            Port = config.Port,
            UserName = config.Username,
            Password = config.Password,
            VirtualHost = !string.IsNullOrWhiteSpace(config.VirtualHost) ? config.VirtualHost : "/",
            DispatchConsumersAsync = true
        };

        this._messageTTLMsecs = config.MessageTTLSecs * 1000;
        this._connection = factory.CreateConnection();
        this._channel = this._connection.CreateModel();
        this._channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        this._consumer = new AsyncEventingBasicConsumer(this._channel);
    }

    /// <inheritdoc />
    public Task<IQueue> ConnectToQueueAsync(string queueName, QueueOptions options = default, CancellationToken cancellationToken = default)
    {
        ArgumentNullExceptionEx.ThrowIfNullOrWhiteSpace(queueName, nameof(queueName), "The queue name is empty");

        if (!string.IsNullOrEmpty(this._queueName))
        {
            throw new InvalidOperationException($"The queue is already connected to `{this._queueName}`");
        }

        this._queueName = queueName;
        this._log.LogDebug("Queue name: {0}", this._queueName);

        this._channel.QueueDeclare(
            queue: this._queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        this._log.LogTrace("Queue ready");

        if (options.DequeueEnabled)
        {
            this._channel.BasicConsume(queue: this._queueName,
                autoAck: false,
                consumer: this._consumer);

            this._log.LogTrace("Enabling dequeue on queue {0}", this._queueName);
        }

        this._poisonQueueName = this._queueName + this._config.PoisonQueueSuffix;
        this._log.LogDebug("Poison queue name: {0}", this._poisonQueueName);

        this._channel.QueueDeclare(
            queue: this._poisonQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        this._log.LogTrace("Poison queue ready");

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

        this.PublishMessage(this._queueName, Encoding.UTF8.GetBytes(message), Guid.NewGuid().ToString("N"), this._messageTTLMsecs, 0);

        return Task.CompletedTask;
    }

    private void PublishMessage(string queueName, ReadOnlyMemory<byte> body, string messageId, int? expirationMsecs, int deliveryCount)
    {
        var properties = this._channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = messageId;

        if (expirationMsecs.HasValue)
        {
            properties.Expiration = $"{expirationMsecs}";
        }

        properties.Headers = new Dictionary<string, object>
        {
            [DeliveryCountHeader] = deliveryCount
        };

        this._log.LogDebug("Sending message: {0} (TTL: {1} secs)...", properties.MessageId, expirationMsecs.HasValue ? expirationMsecs / 1000 : "infinite");

        this._channel.BasicPublish(
            routingKey: queueName,
            body: body,
            exchange: string.Empty,
            basicProperties: properties);

        this._log.LogDebug("Message sent: {0} (TTL: {1} secs)", properties.MessageId, expirationMsecs.HasValue ? expirationMsecs / 1000 : "infinite");
    }

    /// <inheritdoc />
    public void OnDequeue(Func<string, Task<bool>> processMessageAction)
    {
        this._consumer.Received += async (object sender, BasicDeliverEventArgs args) =>
        {
            var dequeueCount = 1;

            try
            {
                if (args.BasicProperties.Headers.TryGetValue(DeliveryCountHeader, out var deliveryCountObj))
                {
                    // The number of dequeue attempts is equal to the number of times the message has been delivered +1.
                    dequeueCount = Convert.ToInt32(deliveryCountObj, CultureInfo.InvariantCulture) + 1;
                }

                if (dequeueCount <= this._config.MaxRetriesBeforePoisonQueue)
                {
                    this._log.LogDebug("Message '{0}' received, expires after {1}ms", args.BasicProperties.MessageId, args.BasicProperties.Expiration);

                    byte[] body = args.Body.ToArray();
                    string message = Encoding.UTF8.GetString(body);

                    bool success = await processMessageAction.Invoke(message).ConfigureAwait(false);
                    if (success)
                    {
                        this._log.LogTrace("Message '{0}' successfully processed, deleting message", args.BasicProperties.MessageId);
                        this._channel.BasicAck(args.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        var backoffDelay = TimeSpan.FromSeconds(1 * dequeueCount);

                        this._log.LogWarning("Message '{0}' failed to process, putting message back in the queue with a delay of {1} msecs",
                            args.BasicProperties.MessageId, backoffDelay.TotalMilliseconds);

                        await Task.Delay(backoffDelay).ConfigureAwait(false);

                        this._channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
                        this.PublishMessage(this._queueName, args.Body, args.BasicProperties.MessageId, this._messageTTLMsecs, dequeueCount);
                    }
                }
                else
                {
                    this._log.LogWarning("Message '{0}' has reached the maximum number of retries, moving to poison queue", args.BasicProperties.MessageId);

                    this._channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
                    this.PublishMessage(this._poisonQueueName, args.Body, args.BasicProperties.MessageId, expirationMsecs: null, dequeueCount);

                    this._log.LogDebug("Message '{0}' moved to poison queue", args.BasicProperties.MessageId);
                }
            }
#pragma warning disable CA1031 // Must catch all to handle queue properly
            catch (Exception e)
            {
                // Exceptions caught by this block:
                // - message processing failed with exception
                // - failed to delete message from queue
                // - failed to unlock message in the queue

                var backoffDelay = TimeSpan.FromSeconds(1 * dequeueCount);
                this._log.LogWarning(e, "Message '{0}' failed to process, putting message back in the queue with a delay of {1} msecs",
                    args.BasicProperties.MessageId, backoffDelay.TotalMilliseconds);

                await Task.Delay(backoffDelay).ConfigureAwait(false);

                // TODO: verify and document what happens if this fails. RabbitMQ should automatically unlock messages.
                this._channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
                this.PublishMessage(this._queueName, args.Body, args.BasicProperties.MessageId, this._messageTTLMsecs, dequeueCount);
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
}
