// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public class AWSS3Config
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AuthTypes
    {
        Unknown = -1,
        AccessKey,
        CredentialChain,
    }

    public AuthTypes Auth { get; set; } = AuthTypes.Unknown;

    /// <summary>
    /// AWS IAM Access Key (aka Key Name)
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// AWS IAM Secret Access Key (aka Password)
    /// </summary>
    public string SecretAccessKey { get; set; } = string.Empty;

    /// <summary>
    /// AWS S3 endpoint, e.g. https://s3.us-west-2.amazonaws.com
    /// You can use S3 compatible services and dev tools like S3 Ninja.
    /// </summary>
    public string Endpoint { get; set; } = "https://s3.amazonaws.com";

    /// <summary>
    /// S3 bucket name
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// When true, uses path-style addressing for S3 requests (e.g., https://s3.example.com/bucket-name/object).
    /// This is required for S3-compatible services like MinIO that do not support virtual-hosted–style URLs.
    /// </summary>
    public bool ForcePathStyle { get; set; } = false;

    public void Validate()
    {
        if (this.Auth == AuthTypes.Unknown)
        {
            throw new ConfigurationException($"Authentication type '{this.Auth}' undefined or not supported");
        }

        if (this.Auth == AuthTypes.AccessKey)
        {
            if (string.IsNullOrWhiteSpace(this.AccessKey))
            {
                throw new ConfigurationException("S3 Access Key is undefined");
            }

            if (string.IsNullOrWhiteSpace(this.SecretAccessKey))
            {
                throw new ConfigurationException("S3 Secret Key Access undefined");
            }
        }

        if (string.IsNullOrWhiteSpace(this.BucketName))
        {
            throw new ConfigurationException("S3 bucket name undefined");
        }

        if (string.IsNullOrWhiteSpace(this.Endpoint))
        {
            throw new ConfigurationException("S3 endpoint name undefined");
        }
    }
}
