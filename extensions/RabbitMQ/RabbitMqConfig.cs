// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public class RabbitMqConfig
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
}
