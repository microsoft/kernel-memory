# Deploying the infrastructure

You can deploy the Kernel Memory infrastructure to Azure by clicking the button below. This will create required resources. We recommend to create a new resource group for this deployment.

Resource that deployed have an opinionated set of configurations. You can modify services on Azure portal or you can reuse Bicep file and provide your customisations `infra/main.bicep`.

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fcherchyk%2Fkernel-memory%2Fdeploy2azure%2Finfra%2Fmain.json)

After deployment is completed, you will see the following resources in your resource group:

- Application Insights
- Container Apps Environment
- Log Analytics workspace
- Search service
- Container App
- Managed Identity
- Storage account

You can start using Kernel Memory immediately after deployment. Use `Application Url` from Container App as Kernel Memory's endpoint. Default API Keys are: `KernelMemoryServiceAuthorizationAccessKey1` and `KernelMemoryServiceAuthorizationAccessKey2`.

> It is highly important to change the default API keys after deployment. You can do this by updating the `KernelMemory__ServiceAuthorization__AccessKey1` and `KernelMemory__ServiceAuthorization__AccessKey2` Environment Variables for the deployed Container App.

Here is an example of how to create a MemoryWebClient and start using Kernel Memory:

```csharp
var memory = new MemoryWebClient("https://km-service-example.example.azurecontainerapps.io", apiKey: "KernelMemoryServiceAuthorizationAccessKey1");

```

Review [examples](../examples/) in this repo. `001-dotnet-WebClient` example project could be great to start with.
