// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory;

[JsonConverter(typeof(StreamStatesConverter))]
public enum StreamStates
{
    // Inform the client the stream ended to an error.
    Error = 0,

    // When streaming, inform the client to discard any previous data
    // and start collecting again using this record as the first one.
    Reset = 1,

    // When streaming, append the current result to the data
    // already received so far.
    Append = 2,

    // Inform the client the end of the stream has been reached
    // and that this is the last record to append.
    Last = 3,
}

#pragma warning disable CA1308
internal sealed class StreamStatesConverter : JsonConverter<StreamStates>
{
    public override StreamStates Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string value = reader.GetString()!;
        return value.ToLowerInvariant() switch
        {
            "error" => StreamStates.Error,
            "reset" => StreamStates.Reset,
            "append" => StreamStates.Append,
            "last" => StreamStates.Last,
            _ => throw new JsonException($"Unknown {nameof(StreamStates)} value: {value}")
        };
    }

    public override void Write(Utf8JsonWriter writer, StreamStates value, JsonSerializerOptions options)
    {
        string serializedValue = value switch
        {
            StreamStates.Error => "error",
            StreamStates.Reset => "reset",
            StreamStates.Append => "append",
            StreamStates.Last => "last",
            _ => throw new JsonException($"Cannot serialize {nameof(StreamStates)} value: {value}")
        };

        writer.WriteStringValue(serializedValue);
    }
}
#pragma warning restore CA1308
