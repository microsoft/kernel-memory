---
nav_order: 1
parent: How-to guides
title: Partitioning & chunking
permalink: /how-to/custom-partitioning
layout: default
---
# Custom Text Partitioning / Chunking

Kernel Memory extracts text from images, pages, and documents, and then partitions the text into
smaller chunks. This partitioning step is essential for efficient processing.

By default, the partitioning process is managed by the
[TextPartitioningHandler](https://github.com/microsoft/kernel-memory/blob/main/service/Core/Handlers/TextPartitioningHandler.cs),
which uses settings defined in
[TextPartitioningOptions](https://github.com/microsoft/kernel-memory/blob/main/service/Core/Handlers/TextPartitioningOptions.cs).

## Partitioning Process

The handler performs the following steps:

1. **Split text into lines**: If a line is too long, it stops and starts a new line.
2. **Form paragraphs**: Concatenate consecutive lines together up to a maximum paragraph size.
3. **Overlap**: When starting a new paragraph, retain a certain number of lines from the previous paragraph.

## Default Settings

The default values used by `TextPartitioningHandler` are:

| Setting          | Value           | Min | Max                    |
|------------------|-----------------|-----|------------------------|
| Paragraph length | 1000 tokens max |  1  | depends on the LLM     |
| Line length      | 300 tokens max  |  1  | [paragraph length]     |
| Overlap          | 100 tokens      |  0  | [paragraph length - 1] |

Lengths are expressed in tokens, which depend on the large language model (LLM) in use and its
tokenization logic. KernelMemoryBuilder allows specifying a custom tokenizer for each LLM during setup.

## Customizing Partitioning Options

Normally, the default settings are much lower than the maximum number of tokens supported by a model.
However, when working with custom models, **some of these might have a lower limit, leading to errors** such as:

{: .warning }
> The configured partition size **(1000 tokens)** is too big for one of the embedding generators in use.
> The max value allowed is **256 tokens**.

To avoid these errors, or to customize Kernel Memory's behavior, you can change KM service configuration:
edit your local configuration file, and override the
[default values](https://github.com/microsoft/kernel-memory/blob/main/service/Service/appsettings.json).

For example, with small models supporting up to 256 tokens, something like this will do:

```json
{
  "KernelMemory": {
    ...
    "DataIngestion": {
      ...
      "TextPartitioning": {
        "MaxTokensPerParagraph": 256,
        "MaxTokensPerLine": 256,
        "OverlappingTokens": 50
      },
  ...
```

If you are using the .NET Serverless Memory, use this code:

```csharp
var memory = new KernelMemoryBuilder()
    .WithOpenAIDefaults(...)
    .WithCustomTextPartitioningOptions(
        new TextPartitioningOptions
        {
            MaxTokensPerParagraph = 256,
            MaxTokensPerLine = 256,
            OverlappingTokens = 50
        })
    .Build<MemoryServerless>();
```
