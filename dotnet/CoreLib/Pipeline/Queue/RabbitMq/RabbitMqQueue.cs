﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Diagnostics;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microsoft.SemanticMemory.Pipeline.Queue.RabbitMq;

public sealed class RabbitMqQueue : IQueue
{
    private readonly ILogger<RabbitMqQueue> _log;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly AsyncEventingBasicConsumer _consumer;
    private string _queueName = string.Empty;

    /// <summary>
    /// Create a new RabbitMQ queue instance
    /// </summary>
    public RabbitMqQueue(RabbitMqConfig config, ILogger<RabbitMqQueue>? log = null)
    {
        this._log = log ?? DefaultLogger<RabbitMqQueue>.Instance;

        // see https://www.rabbitmq.com/dotnet-api-guide.html#consuming-async
        var factory = new ConnectionFactory
        {
            HostName = config.Host,
            Port = config.Port,
            UserName = config.Username,
            Password = config.Password,
            DispatchConsumersAsync = true
        };

        this._connection = factory.CreateConnection();
        this._channel = this._connection.CreateModel();
        this._channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        this._consumer = new AsyncEventingBasicConsumer(this._channel);
    }

    /// <inherit />
    public Task<IQueue> ConnectToQueueAsync(string queueName, QueueOptions options = default, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(queueName))
        {
            throw new ArgumentOutOfRangeException(nameof(queueName), "The queue name is empty");
        }

        if (!string.IsNullOrEmpty(this._queueName))
        {
            throw new InvalidOperationException($"The queue is already connected to `{this._queueName}`");
        }

        this._queueName = queueName;
        this._channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        if (options.DequeueEnabled)
        {
            this._channel.BasicConsume(queue: this._queueName,
                autoAck: false,
                consumer: this._consumer);
        }

        return Task.FromResult<IQueue>(this);
    }

    /// <inherit />
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

        this._log.LogDebug("Sending message...");

        this._channel.BasicPublish(
            routingKey: this._queueName,
            body: Encoding.UTF8.GetBytes(message),
            exchange: string.Empty,
            basicProperties: null);

        this._log.LogDebug("Message sent");

        return Task.CompletedTask;
    }

    /// <inherit />
    public void OnDequeue(Func<string, Task<bool>> processMessageAction)
    {
        this._consumer.Received += async (object sender, BasicDeliverEventArgs args) =>
        {
            try
            {
                this._log.LogDebug("Message '{0}' received expires at {1}", args.BasicProperties.MessageId, args.BasicProperties.Expiration);

                byte[] body = args.Body.ToArray();
                string message = Encoding.UTF8.GetString(body);

                bool success = await processMessageAction.Invoke(message).ConfigureAwait(false);
                if (success)
                {
                    this._log.LogDebug("Message '{0}' dispatch successful, deleting message", args.BasicProperties.MessageId);
                    this._channel.BasicAck(args.DeliveryTag, multiple: false);
                }
                else
                {
                    this._log.LogWarning("Message '{0}' dispatch rejected, putting message back in the queue", args.BasicProperties.MessageId);
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
}
