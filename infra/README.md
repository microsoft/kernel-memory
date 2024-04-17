# Deploying the infrastructure

You can deploy the Kernel Memory infrastructure to Azure by clicking the button below. This will create required
resources. We recommend to create a new resource group for each deployment.

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fmicrosoft%2Fkernel-memory%2Fmain%2Finfra%2Fmain.json)

<details>

<summary>Tips for customizing the deployment</summary>

Resources are deployed with an opinionated set of configurations. You can modify services on Azure portal or you can
reuse and customize the Bicep files starting from [infra/main.bicep](main.bicep).

> [!TIP]
> The `Deploy to Azure` button uses the [infra/main.json](main.json) file, which is a compiled version of
> [infra/main.bicep](main.bicep). Please note that the `main.json` file is not updated automatically when you
> make changes to `main.bicep` file.
>
> You can use the `az bicep build -f main.bicep` command to compile the Bicep file to a json file.
>
> - [Click here](https://learn.microsoft.com/cli/azure/install-azure-cli) for `az` install instructions
> - [Click here](https://learn.microsoft.com/azure/azure-resource-manager/bicep/bicep-cli) for Bicep CLI commands

</details>

After the deployment is complete, you will see the following resources in your resource group:

- Application Insights
- Container Apps Environment
- Log Analytics workspace
- Search service
- Container App
- Managed Identity
- Storage account

You can start using Kernel Memory immediately after deployment. Use `Application Url` from Container App instance page as Kernel Memory's endpoint. Refer [to this screenshot](./images/ACA-ApplicationUrl.png) if you need help finding Application Url value.

Kernel Memory web service is deployed with `AuthenticationType` set to `APIKey` and default API keys are random GUIDs. Each request requires the `Authorization` HTTP header, passing one of the two keys.

> [!WARNING]
> It is highly recommended to change the default API keys after deployment. You can do this by updating the
> `KernelMemory__ServiceAuthorization__AccessKey1` and `KernelMemory__ServiceAuthorization__AccessKey2` > **environment variables** in the Container App.
>
> Refer [to this screenshot](./images/ACA-EnvVar.png) or to the documentation
> page: [Manage environment variables on Azure Container Apps](https://learn.microsoft.com/azure/container-apps/environment-variables?tabs=portal)
> if you need help finding and changing environment variables.

> [!TIP]
> The easiest way to start using Kernel Memory API is to use Swagger UI. You can access it by navigating to
> `{Application Url}/swagger/index.html` in your browser. Replace `km-service-example.example.azurecontainerapps.io`
> with your Application Url value.

Here is an example of how to create a `MemoryWebClient` instance and start using Kernel Memory web service:

```csharp
var memory = new MemoryWebClient(
    "https://km-service-example.example.azurecontainerapps.io",
    apiKey: "...your WebServiceAuthorizationKey1...");
```

We recommend reviewing the [examples](../examples/) included in the repository, e.g. starting from
[001-dotnet-WebClient](../examples/001-dotnet-WebClient).
