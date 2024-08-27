# Example: using the service for Intent Detection

This example demonstrates intent detection using Kernel Memory.
Initially, intents - each with a name (e.g. “account-balance”) and associated examples - are uploaded to Kernel Memory. 
These examples help LLMs match user input to the corresponding intent name.
Next, we query the intent of specific questions. 
Finally, the example code deletes the intents, although in a real application, these would be stored persistently, imported once, and updated as needed with new examples or intent names.

## Running the example locally

The example points to `http://127.0.0.1:9001` so you should start the Kernel Memory Service locally before running this example.
Start `dotnet/Service/Service.csproj`. See `dotnet/Service/README.md` for details.

```csharp
var memory = new MemoryWebClient("http://127.0.0.1:9001/");
```

### Run the example

To run the example, depending on your platform, execute either `run.sh` or `run.cmd` or just `dotnet run`

## Running the example with KM on Azure

You can easily deploy Kernel Memory to Azure by following procedure on [this page](../../infra/README.md).
You will need to update one line in `Program.cs`, pointing KM WebClient to the Azure Public IP created during the deployment, including the API key chosen during KM deployment.

```csharp
var memory = new MemoryWebClient("http://[IP ADDRESS]/", apiKey: "KEY_YOU_PROVIDED_WHEN_DEPLOYING");
```
