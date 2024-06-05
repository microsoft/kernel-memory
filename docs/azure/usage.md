---
nav_order: 3
parent: Kernel Memory in Azure
title: Usage
permalink: /azure/usage
layout: default
---

# Usage

To use Kernel Memory in Azure, you need to have a good understanding of its architecture and how to deploy it to Azure. You can find the architecture guide [here](architecture) and the deployment guide [here](deployment).

{: .highlight }
When deploying Kernel Memory in Azure, keep in mind that there will be costs associated with the resources it uses.

We recommend starting with the [001-dotnet-WebClient](https://github.com/microsoft/kernel-memory/tree/main/examples/001-dotnet-WebClient) sample to explore various aspects of Kernel Memory usage in Azure.

For more information on intent detection with Kernel Memory, refer to the [How To / Intent Detection](../how-to/intent-detection) guide. To learn about achieving multitenancy in Kernel Memory, check out the [How To / Multitenancy](../how-to/multitenancy) guide.

{: .highlight }
It is important to note that Kernel Memory Service is accessible using API Keys that you provide during the deployment process. In the future, Kernel Memory will support Managed Identity as an authentication method.
