// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.KernelMemory.AI.Onnx;

public class OnnxTextGenerationOptions : TextGenerationOptions
{
    /// <summary>
    /// The maximum length of the response that the model will generate. See https://onnxruntime.ai/docs/genai/reference/config.html
    /// </summary>
    new public uint MaxTokens { get; set; } = 2048;

    /// <summary>
    /// The minimum length of the response that the model will generate. See https://onnxruntime.ai/docs/genai/reference/config.html
    /// </summary>
    public uint MinLength { get; set; } = 0;

    /// <summary>
    /// The algorithm used in text generation. Defaults to GreedySearch.
    /// </summary>
    public OnnxSearchType SearchType { get; set; } = OnnxSearchType.GreedySearch;

    /// <summary>
    /// The number of beams to apply when generating the output sequence using beam search.
    /// If NumBeams=1, then generation is performed using greedy search. If NumBeans > 1, then
    /// generation is performed using beam search. A null value implies using TopN search.
    /// </summary>
    public uint? NumBeams { get; set; } = 1;

    /// <summary>
    /// Whether to stop the beam search when at least NumBeams sentences are finished per batch or not. Defaults to false.
    /// </summary>
    public bool EarlyStopping { get; set; } = false;

    /// <summary>
    /// The number of sequences (responses) to generate. Returns the sequences with the highest scores in order.
    /// </summary>
    new public int ResultsPerPrompt { get; set; } = 1;

    /// <summary>
    /// Only includes tokens that fall within the list of the K most probable tokens. Range is 1 to the vocabulary size.
    /// Defaults to 50.
    /// </summary>
    public uint TopK { get; set; } = 50;

    /// <summary>
    /// Only includes the most probable tokens with probabilities that add up to P or higher.
    /// Defaults to 1, which includes all of the tokens. Range is 0 to 1, exclusive of 0.
    /// </summary>
    new public double NucleusSampling { get; set; } = 1.0;

    /// <summary>
    /// Discounts the scores of previously generated tokens if set to a value greater than 1.
    /// Defaults to 1.
    /// </summary>
    public double RepetitionPenalty { get; set; } = 1.0;

    /// <summary>
    /// Controls the length of the output generated. Value less than 1 encourages the generation
    /// to produce shorter sequences. Values greater than 1 encourages longer sequences. Defaults to 1.
    /// </summary>
    public double LengthPenalty { get; set; } = 1.0;
}

/// <summary>
/// An enum representing the possible text generation search types used by OnnxTextGenerator.
/// See https://onnxruntime.ai/docs/genai/reference/config.html#search-combinations for more details.
/// </summary>
public enum OnnxSearchType
{
    /// <summary>
    /// A decoding algorithm that keeps track of the top K sequences at each step. It explores
    /// multiple paths simultaneously, balancing exploration and exploitation. Often results in more
    /// coherent and higher quality text generation than Greedy Search would.
    /// </summary>
    BeamSearch,
    /// <summary>
    /// The default and simplest decoding algorithm. At each step, a token is selected with the highest
    /// probability as the next word in the sequence.
    /// </summary>
    GreedySearch,
    /// <summary>
    /// Combined Top-P (Nucleus) and Top-K Sampling: A decoding algorithm that samples from the top k tokens
    /// with the highest probabilities, while also considering the smallest set of tokens whose cumulative
    /// probability exceeds a threshold p. This approach dynamically balances diversity and coherence in
    /// text generation by adjusting the sampling pool based on both fixed and cumulative probability criteria.
    /// </summary>
    TopN
}
