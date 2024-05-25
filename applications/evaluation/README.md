## KM Evaluation

This repository contains the code for the evaluation of the Knowledge Management (KM) system. The evaluation is based on the following metrics:

- **Faithfulness**: Ensuring the generated text accurately represents the source information.
- **Answer Relevancy**: Assessing the pertinence of the answer in relation to the query.
- **Context Recall**: Measuring the proportion of relevant context retrieved.
- **Context Precision**: Evaluating the accuracy of the retrieved context.
- **Context Relevancy**: Determining the relevance of the provided context to the query.
- **Context Entity Recall**: Checking the retrieval of key entities within the context.
- **Answer Semantic Similarity**: Comparing the semantic similarity between the generated answer and the expected answer.
- **Answer Correctness**: Verifying the factual correctness of the generated answers.


## Usage

### Test set generation

To evaluate the KM, you must first create a test set containing the queries and the expected answers. 
Since this is a manual process, this might be fastidious for large datasets. 
To help you with this task, we provide a generator that creates a test set from a given KM memory and index. 

```csharp
using Microsoft.KernelMemory.Evaluation;

TestSetGenerator testSetGenerator = new(kernel, memoryBuilder);

var distribution = new Distribution();

distribution.Simple = .5f;
distribution.Reasoning = .16f;
distribution.MultiContext = .17f;
distribution.Conditioning = .17f;

var testSet = testSetGenerator.GenerateTestSetsAsync(index: "default", count: 10, retryCount: 3, distribution: distribution);

await foreach (var test in testSet)
{
    Console.WriteLine(test.Question);
}
```


### Evaluation

To evaluate the KM, you can use the following code:

```csharp
var evaluation = new TestSetEvaluator(kernelBuilder, memoryBuilder.Build(), indexName: "default");

var results = evaluation.EvaluateTestSetAsync(await testSet.ToArrayAsync());

await foreach (var result in results)
{
    Console.WriteLine($"Faithfulness: {result.Metrics.Faithfulness}, ContextRecall: {result.Metrics.ContextRecall}");
}
```

## Credits

This project is an implementation of [RAGAS: Evaluation framework for your Retrieval Augmented Generation (RAG) pipelines](https://github.com/explodinggradients/ragas?tab=readme-ov-file).
