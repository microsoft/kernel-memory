// Copyright (c) Microsoft. All rights reserved.

using System;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.SemanticMemory.Core.Pipeline.Queue.AzureQueues;

public static partial class DependencyInjection
{
    public static IServiceCollection AddAzureQueue(this IServiceCollection services, AzureQueueConfig config)
    {
        IQueue QueueFactory(IServiceProvider serviceProvider)
        {
            switch (config.Auth)
            {
                case "":
                case string x when x.Equals("AzureIdentity", StringComparison.OrdinalIgnoreCase):
                    return new AzureQueue(
                        accountName: config.Account,
                        credential: new DefaultAzureCredential(),
                        logger: serviceProvider.GetService<ILogger<AzureQueue>>());

                case string x when x.Equals("ConnectionString", StringComparison.OrdinalIgnoreCase):
                    return new AzureQueue(
                        connectionString: config.ConnectionString,
                        logger: serviceProvider.GetService<ILogger<AzureQueue>>());

                default:
                    throw new NotImplementedException($"Azure Queue auth type '{config.Auth}' not available");
            }
        }

        // The orchestrator uses multiple queue clients, each linked to a specific queue,
        // so it requires a factory rather than a single queue injected to the ctor.
        return services
            .AddSingleton<QueueClientFactory>(serviceProvider => new QueueClientFactory(() => QueueFactory(serviceProvider)));
    }
}
