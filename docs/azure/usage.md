---
nav_order: 3
parent: Kernel Memory in Azure
title: Usage
permalink: /azure/usage
layout: default
---



# Usage

To use Kernel Memory in Azure, you need to have a good understanding of its architecture and how to deploy it to Azure.
You can find the architecture guide [here](architecture) and the deployment guide [here](deployment).

{: .highlight }
When deploying Kernel Memory in Azure, keep in mind that there will be costs associated with the resources it uses.
Refer to the [cost management documentation](https://docs.microsoft.com/en-us/azure/cost-management-billing/cost-management-billing-overview) for more details.

## Getting Started

We recommend starting with the [001-dotnet-WebClient](https://github.com/microsoft/kernel-memory/tree/main/examples/001-dotnet-WebClient) sample to explore various aspects of Kernel Memory usage in Azure.
This sample will help you understand the basic operations and how to interact with the Kernel Memory Service.

## Guides and Documentation

For more information on intent detection with Kernel Memory, refer to the [How To / Intent Detection](../how-to/intent-detection) guide.
To learn about achieving multitenancy in Kernel Memory, check out the [How To / Multitenancy](../how-to/multitenancy) guide.

## Security and Authentication

{: .highlight }
It is important to note that Kernel Memory Service is accessible using API Keys that you provide during the deployment process.
In the future, Kernel Memory will support Managed Identity as an authentication method.

### API Key Management

- Ensure that your API keys are stored securely and not hard-coded in your applications.
- Use environment variables or Azure Key Vault to manage your API keys securely.

### Future Support for Managed Identity

- Managed Identity will provide a more secure and streamlined way to authenticate and access Kernel Memory services without the need for API keys.
- Keep an eye on the [release notes](https://github.com/microsoft/kernel-memory/releases) for updates on when Managed Identity support will be available.

## Best Practices

### Cost Management

- Monitor your Azure usage and costs regularly using Azure Cost Management tools.
- Implement scaling strategies to manage costs effectively, such as auto-scaling and scheduling non-critical resources to shut down during off-peak hours.

### Performance Optimization

- Use Azure Monitor to track the performance of your Kernel Memory deployment.
- Optimize your resource configurations based on usage patterns to ensure efficient performance.

### Maintenance and Updates

- Regularly update your Kernel Memory deployment to take advantage of the latest features and security patches.

## Additional Resources

- [Azure Cost Management and Billing](https://docs.microsoft.com/en-us/azure/cost-management-billing/)
- [Azure Monitor](https://docs.microsoft.com/en-us/azure/azure-monitor/)
