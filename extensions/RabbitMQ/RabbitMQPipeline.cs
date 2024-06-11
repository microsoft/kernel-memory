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
    private string _queueName = string.Empty;
    private string _poisonQueueName = string.Empty;

    /// <summary>
    /// Create a new RabbitMQ queue instance
    /// </summary>
    public RabbitMQPipeline(RabbitMQConfig config, ILogger<RabbitMQPipeline>? log = null)
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
            throw new InvalidOperationException($"The client is already connected to `{this._queueName}`");
        }

        this._queueName = queueName;

        var poisonExchange = $"{this._queueName}.exchange";
        this._channel.ExchangeDeclare(poisonExchange, "fanout");
        this._log.LogTrace("Exchange {0} for dead-letter messages related to queue {1} ready", poisonExchange, this._queueName);

        this._channel.QueueDeclare(
            queue: this._queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-queue-type"] = "quorum",
                ["x-delivery-limit"] = this._config.MaxRetriesBeforePoisonQueue,
                ["x-dead-letter-exchange"] = poisonExchange
            });

        this._log.LogTrace("Queue name: {0}", this._queueName);

        if (options.DequeueEnabled)
        {
            this._channel.BasicConsume(queue: this._queueName,
                autoAck: false,
                consumer: this._consumer);

            this._log.LogTrace("Enabling dequeue on queue `{0}`", this._queueName);
        }

        this._poisonQueueName = this._queueName + this._config.PoisonQueueSuffix;
        this._channel.QueueDeclare(
            queue: this._poisonQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        this._channel.QueueBind(this._poisonQueueName, poisonExchange, string.Empty, null);

        this._log.LogTrace("Poison queue name: {0}", this._poisonQueueName);

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
            try
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
                    this._log.LogWarning("Message '{0}' failed to process, putting message back in the queue", args.BasicProperties.MessageId);
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

                this._log.LogWarning(e, "Message '{0}' processing failed with exception, putting message back in the queue", args.BasicProperties.MessageId);

                // TODO: verify and document what happens if this fails. RabbitMQ should automatically unlock messages.
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
