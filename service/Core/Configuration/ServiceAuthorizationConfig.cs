// Copyright (c) Microsoft. All rights reserved.

using System.Linq;

namespace Microsoft.KernelMemory.Configuration;

public class ServiceAuthorizationConfig
{
    public const string APIKeyAuthType = "APIKey";
    public const int AccessKeyMinLength = 32;

    public static readonly char[] ValidChars =
    {
        ',', '.', ';', ':', '_', '-',
        '!', '@', '#', '$', '^', '*', '~', '=', '|',
        '[', ']', '{', '}', '(', ')'
    };

    /// <summary>
    /// Whether clients must provide some credentials to interact with the HTTP API.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Currently "APIKey" is the only type supported
    /// </summary>
    public string AuthenticationType { get; set; } = APIKeyAuthType;

    /// <summary>
    /// HTTP header name to check for the access key
    /// </summary>
    public string HttpHeaderName { get; set; } = "Authorization";

    /// <summary>
    /// Access Key 1. Alphanumeric, "-" "_" "." allowed. Min 32 chars.
    /// Two different keys are always active, to allow secrets rotation.
    /// </summary>
    public string AccessKey1 { get; set; } = "";

    /// <summary>
    /// Access Key 2. Alphanumeric, "-" "_" "." allowed. Min 32 chars.
    /// Two different keys are always active, to allow secrets rotation.
    /// </summary>
    public string AccessKey2 { get; set; } = "";

    public void Validate()
    {
        if (!this.Enabled)
        {
            return;
        }

        if (this.AuthenticationType != APIKeyAuthType)
        {
            throw new ConfigurationException($"KM Web Service: authorization type '{this.AuthenticationType}' is not supported. Please use '{APIKeyAuthType}'.");
        }

        if (string.IsNullOrWhiteSpace(this.HttpHeaderName))
        {
            throw new ConfigurationException("KM Web Service: the HTTP header name cannot be empty");
        }

        ValidateAccessKey(this.AccessKey1, 1);
        ValidateAccessKey(this.AccessKey2, 2);
    }

    private static void ValidateAccessKey(string key, int keyNumber)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ConfigurationException($"KM Web Service: Access Key {keyNumber} is empty.");
        }

        if (key.Length < AccessKeyMinLength)
        {
            throw new ConfigurationException($"KM Web Service: Access Key {keyNumber} is too short, use at least {AccessKeyMinLength} chars.");
        }

        if (!key.All(IsValidChar))
        {
            throw new ConfigurationException($"KM Web Service: Access Key {keyNumber} contains some invalid chars (allowed: A-B, a-b, 0-9, '{string.Join("', '", ValidChars)}')");
        }
    }

    private static bool IsValidChar(char c)
    {
        return char.IsLetterOrDigit(c) || ValidChars.Contains(c);
    }
}
