// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Configuration;

namespace Microsoft.KernelMemory.MemoryDb.Elasticsearch;

/// <summary>
/// The builder for ElasticsearchConfig.
/// </summary>
public class ElasticsearchConfigBuilder
{
    /// <summary>
    /// The default Elasticsearch endpoint.
    /// </summary>
    public const string DefaultEndpoint = "https://localhost:9200";

    /// <summary>
    /// The default Elasticsearch username.
    /// </summary>
    public const string DefaultUserName = "elastic";

    /// <summary>
    /// The name of the section that will contain the configuration for Elasticsearch
    /// (e.g. appSettings.json, user secrets, etc.).
    /// </summary>
    public const string DefaultSettingsSection = "Elasticsearch";

    /// <summary>
    /// The default prefix to be prepend to the index names in Elasticsearch.
    /// </summary>
    public const string DefaultIndexPrefix = "km.";

    private ElasticsearchConfig _config;

    /// <summary>
    /// The default constructor.
    /// </summary>
    public ElasticsearchConfigBuilder()
    {
        this._config = new ElasticsearchConfig();
        this.WithEndpoint(DefaultEndpoint)
            .WithIndexPrefix(DefaultIndexPrefix)
            .WithCertificateFingerPrint(string.Empty)
            .WithUserNameAndPassword(DefaultUserName, string.Empty);
    }

    /// <summary>
    /// Sets Elasticsearch endpoint to connect to.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    public ElasticsearchConfigBuilder WithEndpoint(string endpoint)
    {
        // TODO: validate URL
        this._config.Endpoint = endpoint;
        return this;
    }

    /// <summary>
    /// Sets the username and password used to connect to Elasticsearch.
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public ElasticsearchConfigBuilder WithUserNameAndPassword(string userName, string password)
    {
        this._config.UserName = userName;
        this._config.Password = password;
        return this;
    }

    /// <summary>
    /// Sets the certificate fingerprint used to communicate with Elasticsearch.
    /// See <see href="https://www.elastic.co/guide/en/elasticsearch/reference/current/configuring-stack-security.html#_use_the_ca_fingerprint_5"/>.
    /// </summary>
    /// <param name="certificateFingerPrint"></param>
    /// <returns></returns>
    public ElasticsearchConfigBuilder WithCertificateFingerPrint(string certificateFingerPrint)
    {
        this._config.CertificateFingerPrint = certificateFingerPrint;
        return this;
    }

    /// <summary>
    /// Sets the prefix to be prepend to the index names in Elasticsearch.
    /// </summary>
    /// <param name="indexPrefix"></param>
    /// <returns></returns>
    public ElasticsearchConfigBuilder WithIndexPrefix(string indexPrefix)
    {
        this._config.IndexPrefix = indexPrefix;
        return this;
    }

    /// <summary>
    /// Validates the Elasticsearch configuration.
    /// </summary>
    /// <returns></returns>
    public ElasticsearchConfigBuilder Validate()
    {
        // TODO: improve this at some point
        const string Prefix = "Invalid Elasticsearch configuration: missing ";

        if (string.IsNullOrWhiteSpace(this._config.Endpoint))
        {
            throw new ConfigurationException(Prefix + $"{nameof(ElasticsearchConfig.Endpoint)}.");
        }

        if (string.IsNullOrWhiteSpace(this._config.UserName))
        {
            throw new ConfigurationException(Prefix + $"{nameof(ElasticsearchConfig.UserName)}.");
        }

        if (string.IsNullOrWhiteSpace(this._config.Password))
        {
            throw new ConfigurationException(Prefix + $"{nameof(ElasticsearchConfig.Password)}.");
        }

        if (string.IsNullOrWhiteSpace(this._config.CertificateFingerPrint))
        {
            throw new ConfigurationException(Prefix + $"{nameof(ElasticsearchConfig.CertificateFingerPrint)}");
        }

        return this;
    }

    /// <summary>
    /// Reads the Elasticsearch configuration from the Services section of KernelMemory's configuration.
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public ElasticsearchConfigBuilder WithConfiguration(IConfiguration configuration)
    {
        const string SectionPath = "KernelMemory:Services:Elasticsearch";

        var kmSvcEsSection = configuration.GetSection(SectionPath);
        if (!kmSvcEsSection.Exists())
        {
            throw new ConfigurationException($"Missing configuration section {SectionPath}.");
        }

        this._config = new ElasticsearchConfig();
        kmSvcEsSection.Bind(this._config);

        configuration.Bind(SectionPath, this._config);

        return this;
    }

    /// <summary>
    /// Sets the number of shards and replicas to use for the Elasticsearch index.
    /// </summary>
    /// <param name="shards"></param>
    /// <param name="replicas"></param>
    /// <returns></returns>
    public ElasticsearchConfigBuilder WithShardsAndReplicas(int shards, int replicas)
    {
        this._config.ShardCount = shards;
        this._config.ReplicaCount = replicas;
        return this;
    }

    /// <summary>
    /// Builds the ElasticsearchConfig.
    /// </summary>
    /// <param name="skipValidation">Indicates if validation should be skipped.</param>
    /// <returns></returns>
    public ElasticsearchConfig Build(bool skipValidation = false)
    {
        if (!skipValidation)
        {
            this.Validate();
        }

        return this._config;
    }
}
