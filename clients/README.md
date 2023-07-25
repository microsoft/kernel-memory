Semantic Memory clients allow to ingest files and create memory records, and to
ask questions, receiving answer grounded on the memory available.

* [curl](curl) contains a CLI client based on `curl`, that allows to upload files
  to the Semantic Memory web service, without the need to run any code.
* [dotnet](dotnet) contains the serverless `MemoryPipelineClient`, that allows
  to process files locally without any deployment, and `MemoryWebclient`, that
  leverages your deploment(s) of the Semantic Memory services.
* [samples](samples) contains some illustrative code showing how to use the code
  above.
