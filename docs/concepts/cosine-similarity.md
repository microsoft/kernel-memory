---
nav_order: 7
parent: Concepts
title: Cosine Similarity
permalink: /concepts/cosine-similarity
layout: default
---
# Cosine Similarity

Cosine similarity is a measure of the degree of similarity between two vectors in
a multidimensional space. It is commonly used in artificial intelligence and natural
language processing to compare [embeddings](embedding),
which are numerical representations of
words or other objects.

The cosine similarity between two vectors is calculated by taking the
[dot product](dot-product) of the two vectors and dividing it by the product
of their magnitudes. This results in a value between -1 and 1, where 1 indicates
that the two vectors are identical, 0 indicates that they are orthogonal
(i.e., have no correlation), and -1 indicates that they are opposite.

$$
A = (A_1, A_2, \ldots, A_n)
$$

$$
B = (B_1, B_2, \ldots, B_n)
$$

$$
\text{Cosine Similarity} = \frac{A \cdot B}{\|A\| \|B\|}
$$

$$
A \cdot B = \sum_{i=1}^{n} A_i B_i
$$

$$
\|A\| = \sqrt{\sum_{i=1}^{n} A_i^2}
$$

$$
\|B\| = \sqrt{\sum_{i=1}^{n} B_i^2}
$$

Cosine similarity is particularly useful when working with high-dimensional data
such as word embeddings because it takes into account both the magnitude and direction
of each vector. This makes it more robust than other measures like
[Euclidean distance](euclidean-distance), which only considers the direction.

One common use case for cosine similarity is to find similar words based on their
embeddings. For example, given an embedding for "cat", we can use cosine similarity
to find other words with similar embeddings, such as "kitten" or "feline". This
can be useful for tasks like text classification or sentiment analysis where we
want to group together semantically related words.

Another application of cosine similarity is in recommendation systems. By representing
items (e.g., movies, products) as vectors, we can use cosine similarity to find
items that are similar to each other or to a particular item of interest. This
allows us to make personalized recommendations based on a user's past behavior
or preferences.

Overall, cosine similarity is an essential tool for developers working with AI
and embeddings. Its ability to capture both magnitude and direction makes it well
suited for high-dimensional data, and its applications in natural language
processing and recommendation systems make it a valuable tool for building
intelligent applications.

## Applications

Some examples about cosine similarity applications.

1. Recommender Systems: Cosine similarity can be used to find similar items or users
   in a recommendation system, based on their embedding vectors.

2. Document Similarity: Cosine similarity can be used to compare the similarity of
   two documents by representing them as embedding vectors and calculating the cosine
   similarity between them.

3. Image Recognition: Cosine similarity can be used to compare the embeddings of
   two images, which can help with image recognition tasks.

4. Natural Language Processing: Cosine similarity can be used to measure the semantic
   similarity between two sentences or paragraphs by comparing their embedding vectors.

5. Clustering: Cosine similarity can be used as a distance metric for clustering
   algorithms, helping group similar data points together.

6. Anomaly Detection: Cosine similarity can be used to identify anomalies in a dataset
   by finding data points that have a low cosine similarity with other data points in
   the dataset.

