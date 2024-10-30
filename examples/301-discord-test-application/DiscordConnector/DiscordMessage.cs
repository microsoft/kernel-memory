// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Microsoft.KernelMemory.Sources.DiscordBot;

public class DiscordMessage
{
    [JsonPropertyOrder(0)]
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyOrder(1)]
    [JsonPropertyName("reference_message_id")]
    public string? ReferenceMessageId { get; set; } = string.Empty;

    [JsonPropertyOrder(2)]
    [JsonPropertyName("author_username")]
    public string? AuthorUsername { get; set; } = string.Empty;

    [JsonPropertyOrder(3)]
    [JsonPropertyName("author_id")]
    public string? AuthorId { get; set; } = string.Empty;

    [JsonPropertyOrder(4)]
    [JsonPropertyName("channel_name")]
    public string? ChannelName { get; set; } = string.Empty;

    [JsonPropertyOrder(5)]
    [JsonPropertyName("channel_id")]
    public string? ChannelId { get; set; } = string.Empty;

    [JsonPropertyOrder(6)]
    [JsonPropertyName("channel_mention")]
    public string? ChannelMention { get; set; } = string.Empty;

    [JsonPropertyOrder(7)]
    [JsonPropertyName("channel_topic")]
    public string? ChannelTopic { get; set; } = string.Empty;

    [JsonPropertyOrder(8)]
    [JsonPropertyName("server_id")]
    public string? ServerId { get; set; } = string.Empty;

    [JsonPropertyOrder(9)]
    [JsonPropertyName("server_name")]
    public string? ServerName { get; set; } = string.Empty;

    [JsonPropertyOrder(10)]
    [JsonPropertyName("server_description")]
    public string? ServerDescription { get; set; } = string.Empty;

    [JsonPropertyOrder(11)]
    [JsonPropertyName("server_member_count")]
    public int ServerMemberCount { get; set; } = 0;

    [JsonPropertyOrder(12)]
    [JsonPropertyName("embeds_count")]
    public int EmbedsCount { get; set; } = 0;

    [JsonPropertyOrder(13)]
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.MinValue;

    [JsonPropertyOrder(14)]
    [JsonPropertyName("content")]
    public string? Content { get; set; } = string.Empty;

    [JsonPropertyOrder(15)]
    [JsonPropertyName("clean_content")]
    public string? CleanContent { get; set; } = string.Empty;
}
