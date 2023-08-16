Notes about Qdrant:

* Record keys (ie point IDs) can only be GUID or INT. Custom strings keys
  are not supported. To use custom string keys the code uses a payload field.
  This forces every upsert to check if a record exists first and to fetch
  the point ID required for sending update requests.

* Similarity search results are sorted alphabetically, not by similarity.
  This forces clients to download all the results and sort them locally
  before returning the list. This also blocks from streaming results.

* Search by payload requires an empty vector even if not used. This forces
  clients searching by payload values to know the size of vectors.