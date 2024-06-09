// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace Microsoft.KernelMemory.SemanticKernelPlugin.Internals;

/// <summary>
/// Type required by Semantic Kernel for mapping
/// </summary>
[TypeConverter(typeof(TypeConverter))]
public class TagCollectionWrapper : TagCollection;

/// <summary>
/// Type required by Semantic Kernel for mapping
/// </summary>
[TypeConverter(typeof(TypeConverter))]
public class ListOfStringsWrapper : List<string>;

#pragma warning disable CA1812 // required by SK
internal sealed class TypeConverter : System.ComponentModel.TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => true;

    /// <summary>
    /// This method is used to convert object from string to actual type. This will allow to pass object to
    /// native function which requires it.
    /// </summary>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
    {
        return value == null ? null : JsonSerializer.Deserialize<TagCollectionWrapper>((string)value);
    }

    /// <summary>
    /// This method is used to convert actual type to string representation, so it can be passed to AI
    /// for further processing.
    /// </summary>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        return JsonSerializer.Serialize(value);
    }
}
#pragma warning restore CA1812
