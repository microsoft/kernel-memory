Notes about Azure Cognitive Search:

* Pre-filtering is not supported yet, and when searching using both vector
  search and filters, vector search is applying before applying the remaining
  filters. This requires clients to increase the value of "limit" ("top")
  to allow for vectors to be filtered out without leaving an empty result set.

* Currently, the code does not support hybrid search combining vector search
  with full text search.