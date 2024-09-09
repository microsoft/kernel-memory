// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.AI.Ollama;

public class OllamaModelConfig
{
    /// <summary>
    /// Model used for text generation. Chat models can be used too.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// The max number of tokens supported by the model.
    /// Default to 4096 for text and 8192 for embeddings.
    /// </summary>
    public int? MaxTokenTotal { get; set; }

    /// <summary>
    /// Enable Mirostat sampling for controlling perplexity.
    /// (default: 0, 0 = disabled, 1 = Mirostat, 2 = Mirostat 2.0)
    /// </summary>
    public int? MiroStat { get; set; }

    /// <summary>
    /// Influences how quickly the algorithm responds to feedback from the
    /// generated text. A lower learning rate will result in slower adjustments,
    /// while a higher learning rate will make the algorithm more responsive.
    /// (Default: 0.1)
    /// </summary>
    public float? MiroStatEta { get; set; }

    /// <summary>
    /// Controls the balance between coherence and diversity of the output.
    /// A lower value will result in more focused and coherent text.
    /// (Default: 5.0)
    /// </summary>
    public float? MiroStatTau { get; set; }

    /// <summary>
    /// Sets the size of the context window used to generate the next token.
    /// (Default: 2048)
    /// </summary>
    public int? NumCtx { get; set; }

    /// <summary>
    /// The number of GQA groups in the transformer layer. Required for some
    /// models, for example it is 8 for llama2:70b
    /// </summary>
    public int? NumGqa { get; set; }

    /// <summary>
    /// The number of layers to send to the GPU(s). On macOS it defaults to
    /// 1 to enable metal support, 0 to disable.
    /// </summary>
    public int? NumGpu { get; set; }

    /// <summary>
    /// Sets the number of threads to use during computation. By default,
    /// Ollama will detect this for optimal performance.
    /// It is recommended to set this value to the number of physical CPU cores
    /// your system has (as opposed to the logical number of cores).
    /// </summary>
    public int? NumThread { get; set; }

    /// <summary>
    /// Sets how far back for the model to look back to prevent repetition.
    /// (Default: 64, 0 = disabled, -1 = num_ctx)
    /// </summary>
    public int? RepeatLastN { get; set; }

    /// <summary>
    /// Sets the random number seed to use for generation.
    /// Setting this to a specific number will make the model generate the same
    /// text for the same prompt. (Default: 0)
    /// </summary>
    public int? Seed { get; set; }

    /// <summary>
    /// Tail free sampling is used to reduce the impact of less probable
    /// tokens from the output. A higher value (e.g., 2.0) will reduce the
    /// impact more, while a value of 1.0 disables this setting. (default: 1)
    /// </summary>
    public float? TfsZ { get; set; }

    /// <summary>
    /// Maximum number of tokens to predict when generating text.
    /// (Default: 128, -1 = infinite generation, -2 = fill context)
    /// </summary>
    public int? NumPredict { get; set; }

    /// <summary>
    /// Reduces the probability of generating nonsense. A higher value
    /// (e.g. 100) will give more diverse answers, while a lower value (e.g. 10)
    /// will be more conservative. (Default: 40)
    /// </summary>
    public int? TopK { get; set; }

    /// <summary>
    /// Alternative to the top_p, and aims to ensure a balance of quality and variety.min_p represents the minimum
    /// probability for a token to be considered, relative to the probability of the most likely token.For
    /// example, with min_p=0.05 and the most likely token having a probability of 0.9, logits with a value less
    /// than 0.05*0.9=0.045 are filtered out. (Default: 0.0)
    /// </summary>
    public float? MinP { get; set; }

    /// <summary>
    /// How many requests can be processed in parallel
    /// </summary>
    public int MaxBatchSize { get; set; } = 1;

    public OllamaModelConfig()
    {
    }

    public OllamaModelConfig(string modelName)
    {
        this.ModelName = modelName;
    }

    public OllamaModelConfig(string modelName, int maxToken)
    {
        this.ModelName = modelName;
        this.MaxTokenTotal = maxToken;
    }
}
