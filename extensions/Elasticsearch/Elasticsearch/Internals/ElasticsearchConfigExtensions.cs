// Copyright (c) Microsoft. All rights reserved.

using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace Microsoft.KernelMemory.MemoryDb.Elasticsearch.Internals;

/// <summary>
/// Elasticsearch configuration extensions.
/// </summary>
internal static class ElasticsearchConfigExtensions
{
    /// <summary>
    /// Converts an ElasticsearchConfig to a ElasticsearchClientSettings that can be used
    /// to instantiate <see cref="ElasticsearchClient"/>.
    /// </summary>
    public static ElasticsearchClientSettings ToElasticsearchClientSettings(this ElasticsearchConfig config)
    {
        ArgumentNullExceptionEx.ThrowIfNull(config, nameof(config), "The configuration is NULL");

        // TODO: figure out the Dispose issue. It does not feel right.
        // See https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/_options_on_elasticsearchclientsettings.html
#pragma warning disable CA2000 // Dispose objects before losing scope
        return new ElasticsearchClientSettings(new Uri(config.Endpoint))

                // TODO: this needs to be more flexible.
                .Authentication(new BasicAuthentication(config.UserName, config.Password))
                .DisableDirectStreaming(true)
                // TODO: Not sure why I need this. Verify configuration maybe?
                .ServerCertificateValidationCallback((sender, certificate, chain, errors) => true)
                .CertificateFingerprint(config.CertificateFingerPrint)
                .ThrowExceptions(true) // Much easier to work with
#if DEBUG
                .DisableDirectStreaming(true)
#endif
            ;
#pragma warning restore CA2000 // Dispose objects before losing scope
    }
}
