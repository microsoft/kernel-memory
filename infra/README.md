# Deploying the infrastructure

You can deploy the Kernel Memory infrastructure to Azure by clicking the button below. This will create required resources. We recommend to create a new resource group for each deployment.

Resources are deployed with an opinionated set of configurations. You can modify services on Azure portal or you can reuse and customize the Bicep files starting from [infra/main.bicep](main.bicep).

> The following `Deploy to Azure` button uses the [infra/main.json](main.json) file which is a compiled version of
> [infra/main.bicep](main.bicep). Please note that the `main.json` file is not updated automatically when you
> make changes to `main.bicep` file. You can use the `bicep build main.bicep` command to compile the bicep
> file to a json file.

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fcherchyk%2Fkernel-memory%2Fdeploy2azure%2Finfra%2Fmain.json)

After deployment is completed, you will see the following resources in your resource group:

- Application Insights
- Container Apps Environment
- Log Analytics workspace
- Search service
- Container App
- Managed Identity
- Storage account

You can start using Kernel Memory immediately after deployment. Use `Application Url` from Container App instance page as Kernel Memory's endpoint. Refer [to this screenshot](./images/ACA-ApplicationUrl.png) if you need help finding Application Url value.

Kernel Memory infrastructure that is deployed requires the AuthenticationType using 'APIKey'. The default API keys are `KernelMemoryServiceAuthorizationAccessKey1` and `KernelMemoryServiceAuthorizationAccessKey2`.

> The easiest way to start using Kernel Memory API is to use Swagger UI. You can access it by navigating to `{Application Url}/swagger/index.html` in your browser. Replace `km-service-example.example.azurecontainerapps.io` with your Application Url value.

> It is highly recommended to change the default API keys after deployment. You can do this by updating the `KernelMemory__ServiceAuthorization__AccessKey1` and `KernelMemory__ServiceAuthorization__AccessKey2` environment variables for the deployed Container App. Refer [to this screenshot](./images/ACA-EnvVar.png) or to the documentation page: [Manage environment variables on Azure Container Apps](https://learn.microsoft.com/azure/container-apps/environment-variables?tabs=portal) if you need help finding and changing environment variables.

Here is an example of how to create a MemoryWebClient and start using Kernel Memory:

```csharp
var memory = new MemoryWebClient("https://km-service-example.example.azurecontainerapps.io", apiKey: "KernelMemoryServiceAuthorizationAccessKey1");
```

Review [examples](../examples/) in this repo. `001-dotnet-WebClient` example project could be great to start with.
