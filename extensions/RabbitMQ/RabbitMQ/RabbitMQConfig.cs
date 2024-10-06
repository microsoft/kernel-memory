// Copyright (c) Microsoft. All rights reserved.

using System.Text;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public class RabbitMQConfig
{
    /// <summary>
    /// RabbitMQ hostname, e.g. "127.0.0.1"
    /// </summary>
    public string Host { get; set; } = "";

    /// <summary>
    /// TCP port for the connection, e.g. 5672
    /// </summary>
    public int Port { get; set; } = 0;

    /// <summary>
    /// Authentication username
    /// </summary>
    public string Username { get; set; } = "";

    /// <summary>
    /// Authentication password
    /// </summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// RabbitMQ virtual host name, e.g. "/"
    /// See https://www.rabbitmq.com/docs/vhosts
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// How long to retry messages delivery, ie how long to retry, in seconds.
    /// Default: 3600 second, 1 hour.
    /// </summary>
    public int MessageTTLSecs { get; set; } = 3600;

    /// <summary>
    /// Set to true if your RabbitMQ supports SSL.
    /// Default: false
    /// </summary>
    public bool SslEnabled { get; set; } = false;

    /// <summary>
    /// How many times to retry processing a message before moving it to a poison queue.
    /// Example: a value of 20 means that a message will be processed up to 21 times.
    /// Note: this value cannot be changed after queues have been created. In such case
    ///       you might need to drain all queues, delete them, and restart the ingestion service(s).
    /// </summary>
    public int MaxRetriesBeforePoisonQueue { get; set; } = 20;

    /// <summary>
    /// Suffix used for the poison queues.
    /// </summary>
    public string PoisonQueueSuffix { get; set; } = "-poison";

    /// <summary>
    /// Verify that the current state is valid.
    /// </summary>
    public void Validate()
    {
        const int MinTTLSecs = 5;

        if (string.IsNullOrWhiteSpace(this.Host) || this.Host != $"{this.Host}".Trim())
        {
            throw new ConfigurationException($"RabbitMQ: {nameof(this.Host)} cannot be empty or have leading or trailing spaces");
        }

        if (this.Port < 1)
        {
            throw new ConfigurationException($"RabbitMQ: {nameof(this.Port)} value {this.Port} is not valid");
        }

        if (this.MessageTTLSecs < MinTTLSecs)
        {
            throw new ConfigurationException($"RabbitMQ: {nameof(this.MessageTTLSecs)} value {this.MessageTTLSecs} is too low, cannot be less than {MinTTLSecs}");
        }

        if (string.IsNullOrWhiteSpace(this.PoisonQueueSuffix) || this.PoisonQueueSuffix != $"{this.PoisonQueueSuffix}".Trim())
        {
            throw new ConfigurationException($"RabbitMQ: {nameof(this.PoisonQueueSuffix)} cannot be empty or have leading or trailing spaces");
        }

        if (this.MaxRetriesBeforePoisonQueue < 0)
        {
            throw new ConfigurationException($"RabbitMQ: {nameof(this.MaxRetriesBeforePoisonQueue)} cannot be a negative number");
        }

        if (string.IsNullOrWhiteSpace(this.PoisonQueueSuffix))
        {
            throw new ConfigurationException($"RabbitMQ: {nameof(this.PoisonQueueSuffix)} is empty");
        }

        // Queue names can be up to 255 bytes of UTF-8 characters.
        // Allow a max of 60 bytes for the suffix, so there is room for the queue name.
        if (Encoding.UTF8.GetByteCount(this.PoisonQueueSuffix) > 60)
        {
            throw new ConfigurationException($"RabbitMQ: {nameof(this.PoisonQueueSuffix)} can be up to 60 characters length");
        }
    }
}
