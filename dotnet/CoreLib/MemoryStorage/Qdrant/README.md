Notes about Qdrant:

* Record keys (point IDs) are limited to GUID or INT formats. To utilize
  custom string keys, each point has a custom "id" payload field used to
  identify records. This methods requires one extra request during upsert
  operations, to obtain the point ID required.
