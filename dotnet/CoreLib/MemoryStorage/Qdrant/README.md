Notes about Qdrant:

* Record keys (point IDs) are limited to GUID or INT formats. To utilize
  custom string keys, each point has a custom "id" payload field used to
  identify records. This methods requires one extra request during upsert
  operations, to obtain the point ID required.

* Qdrant returns similarity search results sorted in alphabetical order,
  rather than by similarity. To present results sorted by relevance, starting
  from the most relevant, all search results are downloaded and sorted in
  memory first, before being returned to the user.