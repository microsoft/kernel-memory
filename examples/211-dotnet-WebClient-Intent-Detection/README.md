# Example: using the service for Intent Detection

This example shows how intent detection could be achieved using Kernel Memory WebClient. First, we upload some intents with their samples to Kernel Memory. Then, we ask for the intent of some questions. Finally, we delete the memories we uploaded.

## Running the example locally

The example points to `http://127.0.0.1:9001` so you should start the Kernel Memory Service locally before running this example.
Start `dotnet/Service/Service.csproj`. See `dotnet/Service/README.md` for details.

```csharp
var memory = new MemoryWebClient("http://127.0.0.1:9001/");
```

### Run the example

To run the example, depending on your platform, execute either `run.sh` or `run.cmd` or just `dotnet run`

## Running the example with KM on Azure

You can easily deploy Kernel Memory to Azure by following procedure on [this page](../../infra/README.md). Next, you will need to update one line in `Program.cs` to point to the Azure Public IP created during the deployment, you will also need to provide the API key you used when deploying the service.

```csharp
var memory = new MemoryWebClient("http://[IP ADDRESS]/", apiKey: "KEY_YOU_PROVIDED_WHEN_DEPLOYING");
```
