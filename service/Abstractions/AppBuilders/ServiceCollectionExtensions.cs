// Copyright (c) Microsoft. All rights reserved.

using System.Linq;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130

public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Check if the service collection contains a descriptor for the given type
    /// </summary>
    /// <param name="services">Service Collection</param>
    /// <typeparam name="T">Type required</typeparam>
    /// <returns>True when the service collection contains T</returns>
    public static bool HasService<T>(this IServiceCollection services)
    {
        return (services.Any<ServiceDescriptor>(x => x.ServiceType == typeof(T)));
    }
}
