Notes about Azure Cognitive Search:

* Pre-filtering is not supported yet, and when searching using both vector
  search and filters, vector search is applying before applying the remaining
  filters. This requires clients to increase the value of "limit" ("top")
  to allow for vectors to be filtered out without leaving an empty result set.

* Hybrid search, combining vector search with full text search, is not
  implemented at this stage.