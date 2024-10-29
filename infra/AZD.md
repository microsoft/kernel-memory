# Deploying Kernel Memory infrastructure to Azure using AZD (Azure Developer CLI)

To deploy the Kernel Memory infrastructure to Azure, you can also use the azd up command from
[Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/overview).
This will create all the necessary resources.

## Deployment

The deployment process may take up to 20 minutes.

First you will need to install AZD, you can find how to do this on different platforms on this
[Install or update the Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd).
For example, on Windows you can use this command:

    winget install microsoft.azd

After AZD installation, you'll need to log in with your Azure Account and then you can start the deployment.

To authenticate, execute:

    azd auth login

It'll open Azure's login screen. Complete your authentication process. Remember that depending on your Tenant configuration
Two-Factor Authentication may be required.

### Before starting the deployment for the first time

Kernel Memory deployment bicep uses Resource Group Scoped Deployment feature, and it has to be enabled using this command:

    azd config set alpha.resourceGroupDeployments on

### Deployment

To start the actual deployment of Kernel Memory, navigate to the root of your local clone of the Kernel Memory repository
and execute:

    azd up

After hitting Enter, you will need to fill parameters. 

> You can find more info about parameters here: [README.md](README.md).

The deployment takes about 20 minutes to complete.

### Clean Up Resources

To clean up resources and uninstall Kernel Memory from your Subscription, execute this command:

    azd down

You'll be asked to confirm the deletion of the resource group, and after that you'll be asked to confirm deletion of
your Azure AI Services. After that, all resources will be deleted.

## AZD Resources

> You can find more information about AZD on these links:
> - [Installation guide](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd) for Azure Developer CLI.
> - [Quickstart:](https://learn.microsoft.com/azure/developer/azure-developer-cli/get-started) Deploy an Azure Developer CLI template.
