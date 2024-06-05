---
nav_order: 2
parent: Kernel Memory in Azure
title: Deployment
permalink: /azure/deployment
layout: default
---

# Deployment

Prior to using Kernel Memory in Azure, it is important to understand its architecture. You can find the architecture guide [here](architecture).

The Kernel Memory opinionated deployment could be done following [infra readme instructions](https://github.com/microsoft/kernel-memory/tree/main/infra).

Before using you will need to deploy using ["Deploy to Azure" button](https://github.com/microsoft/kernel-memory/blob/main/infra) or by running bicep from the CLI using `az bicep build -f main.bicep`

{: .highlight }
It's important to understand that Azure resource usage is billed based on the resources you use. Refer to the [cost](architecture#cost) section to understand the costs associated with Kernel Memory.

Kernel Memory takes about 5 minutes to deploy and start using. When you experiment with Kernel Memory it's important to delete the resources after usage to minimize costs. All uploaded data will be lost when resources are deleted.

## Deployment Customization

Infrastructure as code (IaC) is a key part of Kernel Memory deployment. The deployment can be customized by modifying the `.bicep` files in `https://github.com/microsoft/kernel-memory/tree/main/infra`.
If Bicep or IaC is new to you, you can learn more about it in [Fundamentals of Bicep](https://learn.microsoft.com/training/paths/fundamentals-bicep/).
