// Copyright (c) Microsoft. All rights reserved.

using System.Text;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Orchestration.RabbitMQ;
using Microsoft.KernelMemory.Pipeline.Queue;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microsoft.RabbitMQ.TestApplication;

internal static class Program
{
    private const string QueueName = "test queue";

    public static async Task Main()
    {
        var cfg = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.development.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var rabbitMQConfig = cfg.GetSection("KernelMemory:Services:RabbitMQ").Get<RabbitMQConfig>();
        ArgumentNullExceptionEx.ThrowIfNull(rabbitMQConfig, nameof(rabbitMQConfig), "RabbitMQ config not found");

        DefaultLogger.Factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        var pipeline = new RabbitMQPipeline(rabbitMQConfig, DefaultLogger.Factory);

        var counter = 0;
        pipeline.OnDequeue(async msg =>
        {
            Console.WriteLine($"{++counter} Received message: {msg}");
            await Task.Delay(0);
            return false;
        });

        await pipeline.ConnectToQueueAsync(QueueName, QueueOptions.PubSub);

        ListenToDeadLetterQueue(rabbitMQConfig);

        // Change ConcurrentThreads and PrefetchCount to 1 to see
        // how they affect total execution time
        for (int i = 1; i <= 3; i++)
        {
            await pipeline.EnqueueAsync($"test #{i} {DateTimeOffset.Now:T}");
        }

        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }

    private static void ListenToDeadLetterQueue(RabbitMQConfig config)
    {
        var factory = new ConnectionFactory
        {
            HostName = config.Host,
            Port = config.Port,
            UserName = config.Username,
            Password = config.Password,
            VirtualHost = !string.IsNullOrWhiteSpace(config.VirtualHost) ? config.VirtualHost : "/",
            DispatchConsumersAsync = true,
            Ssl = new SslOption
            {
                Enabled = config.SslEnabled,
                ServerName = config.Host,
            }
        };

        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.Received += async (object sender, BasicDeliverEventArgs args) =>
        {
            byte[] body = args.Body.ToArray();
            string message = Encoding.UTF8.GetString(body);

            Console.WriteLine($"Poison message received: {message}");
            await Task.Delay(0);
        };

        channel.BasicConsume(queue: $"{QueueName}{config.PoisonQueueSuffix}",
            autoAck: true,
            consumer: consumer);
    }
}
