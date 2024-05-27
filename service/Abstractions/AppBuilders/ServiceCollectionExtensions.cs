// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Linq;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130

[Experimental("KMEXP00")]
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
