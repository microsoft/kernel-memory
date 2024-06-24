---
nav_order: 2
parent: Kernel Memory in Azure
title: Deployment
permalink: /azure/deployment
layout: default
---

# Deployment

Before using Kernel Memory in Azure, it is important to understand its architecture. You can find the architecture guide [here](architecture).

For the opinionated deployment of Kernel Memory, follow the instructions in the infra [README](https://github.com/microsoft/kernel-memory/tree/main/infra).

To deploy, use the ["Deploy to Azure"](https://github.com/microsoft/kernel-memory/blob/main/infra) button or run the following command in the CLI:

```shell
az bicep build -f main.bicep
```

{: .highlight }
It's important to understand that Azure resource usage is billed based on the resources you use. Refer to the [cost](architecture#cost) section to understand the costs associated with Kernel Memory.

Kernel Memory takes about 5 minutes to deploy and start using. To minimize costs while experimenting with Kernel Memory, delete resources after use. Note that all uploaded data will be lost upon deletion.

## Deployment Customization

Infrastructure as Code (IaC) is a key part of Kernel Memory deployment. You can customize the deployment by modifying the .`bicep` files in the [Kernel Memory GitHub repository](https://github.com/microsoft/kernel-memory/tree/main/infra). If Bicep or IaC is new to you, learn more about it in the [Fundamentals of Bicep](https://learn.microsoft.com/training/paths/fundamentals-bicep/).
