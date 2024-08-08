@description('Suffix to create unique resource names; 4-6 characters. Default is a random 6 characters.')
@minLength(4)
@maxLength(6)
param suffix string = substring(newGuid(), 0, 6)

@description('The tags to apply to all resources. Refer to https://learn.microsoft.com/azure/cloud-adoption-framework/ready/azure-best-practices/naming-and-tagging for best practices.')
param tags object = {
  Application: 'Kernel-Memory'
  Environment: 'Demo'
}

@description('''
Kernel Memory Docker Image Tag.  Check available tags at https://hub.docker.com/r/kernelmemory/service/tags
''')
@minLength(3)
@maxLength(16)
param KernelMemoryImageTag string = 'latest'

///////////////////////////// AI Model Params ///////////////////////////////

@description('''
ATTENTION: USE MODELS THAT YOUR AZURE SUBSCRIPTION IS ALLOWED TO USE.


Azure OpenAI Inference Model. https://learn.microsoft.com/en-gb/azure/ai-services/openai/concepts/models

Default model version will be assigned. The default version is different for different models and might change when there is new version available for a model.
''')
@allowed([
  'gpt-35-turbo-16k'
  'gpt-4'
  'gpt-4-32k'
  'gpt-4o'
  'gpt-4o-mini'
])
param InferenceModel string = 'gpt-35-turbo-16k'

@description('''
Inference deployment model\'s Tokens-Per-Minute (TPM) capacity, measured in thousands.
The default capacity is 30 that represents 30,000 TPM. 
For model limits specific to your region, refer to the documentation at https://learn.microsoft.com/azure/ai-services/openai/concepts/models#standard-deployment-model-quota.
''')
@minValue(1)
@maxValue(40)
param InferenceModelDeploymentCapacity int = 30

@description('''
ATTENTION: USE MODELS THAT YOUR AZURE SUBSCRIPTION IS ALLOWED TO USE.

Azure OpenAI Embedding Model. https://learn.microsoft.com/azure/ai-services/openai/concepts/models#embeddings

Default model version will be assigned. The default version is different for different models and might change when there is new version available for a model.
''')
@allowed([
  'text-embedding-ada-002'
  'text-embedding-3-small'
  'text-embedding-3-large'
])
param EmbeddingModel string = 'text-embedding-ada-002'

@description('''
Embedding deployment model\'s Tokens-Per-Minute (TPM) capacity, measured in thousands.
The default capacity is 30 that represents 30,000 TPM.
For model limits specific to your region, refer to the documentation at https://learn.microsoft.com/azure/ai-services/openai/concepts/models#standard-deployment-model-quota.
''')
@minValue(1)
@maxValue(40)
param EmbeddingModelDeploymentCapacity int = 30

///////////////////////////// App Keys ///////////////////////////////

@description('''
PLEASE CHOOSE A SECURE AND SECRET KEY ! -
Kernel Memory Service Authorization AccessKey 1.
The value is stored as an environment variable and is required by the web service to authenticate HTTP requests.
''')
@minLength(32)
@maxLength(128)
@secure()
param WebServiceAuthorizationKey1 string

@description('''
PLEASE CHOOSE A SECURE AND SECRET KEY ! -
Kernel Memory Service Authorization AccessKey 2.
The value is stored as an environment variable and is required by the web service to authenticate HTTP requests.
''')
@minLength(32)
@maxLength(128)
@secure()
param WebServiceAuthorizationKey2 string

///////////////////////////// Networking Params ///////////////////////////////

@description('''
Define the address space of your virtual network. Refer to the documentation at https://learn.microsoft.com/azure/virtual-network/concepts-and-best-practices
''')
param VirtualNetworkAddressSpace string = '10.0.0.0/16'

@description('''
Select an address space and configure your subnet for Infrastructure. You can also customize a subnet later. Refer to the documentation at https://learn.microsoft.com/azure/virtual-network/virtual-network-vnet-plan-design-arm#subnets
''')
param InfrastructureSubnetAddressRange string = '10.0.0.0/23'

@description('''
Select an address space and configure your subnet for Application Gateway. You can also customize a subnet later. Refer to the documentation at https://learn.microsoft.com/azure/virtual-network/virtual-network-vnet-plan-design-arm#subnets
''')
param ApplicationGatewaySubnetAddressRange string = '10.0.2.0/24'

@description('''
Select an address space and configure your subnet for Private Endpoints. You can also customize a subnet later. Refer to the documentation at https://learn.microsoft.com/azure/virtual-network/virtual-network-vnet-plan-design-arm#subnets
''')
param PrivateEndpointSubnetAddressRange string = '10.0.3.0/24'

/////////////////////////////////////////////////////////////////////////////

var rg = resourceGroup()

var location = resourceGroup().location

/////////////////////////////////////////////////////////////////////////////

module module_vnet 'modules/network/virtual-network.bicep' = {
  name: 'module-vnet-${suffix}'
  params: {
    location: location
    tags: tags
    vnetName: 'km-vnet-${suffix}'

    VirtualNetworkAddressSpace: VirtualNetworkAddressSpace
    InfrastructureSubnetAddressRange: InfrastructureSubnetAddressRange
    ApplicationGatewaySubnetAddressRange: ApplicationGatewaySubnetAddressRange
    PrivateEndpointSubnetAddressRange: PrivateEndpointSubnetAddressRange
  }
}

/*
  Module to create a Managed Identity.
  See https://learn.microsoft.com/entra/identity/managed-identities-azure-resources/overview
  
  The managed identity is the main code-to-services and service-to-service authentication mechanism.
*/
module module_managedidentity 'modules/identity/managed-identity.bicep' = {
  name: 'module-managedidentity-${suffix}'
  scope: rg
  params: {
    location: location
    suffix: suffix
  }
}

/* 
  Module to create a Storage Account
  See https://learn.microsoft.com/azure/storage/common/storage-account-overview
  
  The storage account is used to store files (KM Document Storage) and
  to run asynchronous ingestion (KM Pipelines Orchestration).
*/
module module_storage 'modules/storage.bicep' = {
  name: 'module-storage-${suffix}'
  scope: rg
  params: {
    location: location
    tags: tags
    suffix: suffix

    vnetId: module_vnet.outputs.vnetId
    privateEndpointSubnetId: module_vnet.outputs.privateEndpointSubnetId

    managedIdentityPrincipalId: module_managedidentity.outputs.managedIdentityPrincipalId
  }
}

/*
  Module to create a Azure AI Search service
  See https://azure.microsoft.com/products/ai-services/ai-search
  
  Azure AI Search is used to store document chunks and LLM embeddings, and to search
  for relevant data when searching memories and asking questions.
*/
module module_search 'modules/cognitive/ai-search.bicep' = {
  name: 'module-search-${suffix}'
  scope: rg
  params: {
    location: location
    tags: tags
    suffix: suffix

    vnetId: module_vnet.outputs.vnetId
    privateEndpointSubnetId: module_vnet.outputs.privateEndpointSubnetId

    managedIdentityPrincipalId: module_managedidentity.outputs.managedIdentityPrincipalId
  }
}

/*
  Module to create a Azure OpenAI service
  See https://azure.microsoft.com/products/ai-services/openai-service
      and https://github.com/Azure-Samples/azure-search-openai-demo/blob/main/infra/main.bicep for more details
  
  Azure OpenAI is used to generate text embeddings, and to generate text from memories (answers and summaries)
*/
var InferenceDeploymentName = 'chat'
var EmbeddingDeploymentName = 'embedding'

var openAiDeployments = [
  {
    name: InferenceDeploymentName
    model: {
      format: 'OpenAI'
      name: InferenceModel
      // version: chatGpt.deploymentVersion
    }
    sku: {
      name: 'Standard'
      capacity: InferenceModelDeploymentCapacity
    }
  }
  {
    name: EmbeddingDeploymentName
    model: {
      format: 'OpenAI'
      name: EmbeddingModel
      // version: embedding.deploymentVersion
    }
    sku: {
      name: 'Standard'
      capacity: EmbeddingModelDeploymentCapacity
    }
  }
]
module module_openAi 'modules/cognitive/openAI.bicep' = {
  name: 'module-openai-${suffix}'
  scope: rg
  params: {
    suffix: suffix
    tags: tags

    vnetId: module_vnet.outputs.vnetId
    privateEndpointSubnetId: module_vnet.outputs.privateEndpointSubnetId

    managedIdentityPrincipalId: module_managedidentity.outputs.managedIdentityPrincipalId

    name: 'km-openai-${suffix}'
    location: location
    sku: {
      name: 'S0'
    }
    publicNetworkAccess: 'Disabled'
    deployments: openAiDeployments
  }
}

/*
  Module to create a Azure Document Intelligence service
  See https://azure.microsoft.com/products/ai-services/ai-document-intelligence
  Azure Document Intelligence is used to extract text from images
*/
module module_docIntel 'modules/cognitive/docIntel.bicep' = {
  name: 'module-docIntel-${suffix}'
  scope: rg
  params: {
    suffix: suffix
    tags: tags

    vnetId: module_vnet.outputs.vnetId
    privateEndpointSubnetId: module_vnet.outputs.privateEndpointSubnetId

    managedIdentityPrincipalId: module_managedidentity.outputs.managedIdentityPrincipalId

    publicNetworkAccess: 'Disabled'
    name: 'km-docIntel-${suffix}'
    location: location
    sku: {
      name: 'S0'
    }
  }
}

/* 
  Module to create monitoring resources
*/
module module_insights 'modules/monitoring/insights.bicep' = {
  name: 'module-insights-${suffix}'
  scope: rg
  params: {
    suffix: suffix
    location: location
    tags: tags
  }
}

/* 
  Module to create an Azure Container Apps environment and a container app
  See https://learn.microsoft.com/en-us/azure/container-apps/environment
      and https://azure.github.io/aca-dotnet-workshop/aca/10-aca-iac-bicep/iac-bicep/#2-define-an-azure-container-apps-environment for more samples
*/
module module_containerAppsEnvironment 'modules/host/container-app-env.bicep' = {
  name: 'module-containerAppsEnvironment-${suffix}'
  scope: rg
  params: {
    location: location
    suffix: suffix
    tags: tags
    // network
    acaSubnetId: module_vnet.outputs.envInfraSubnetId
    logAnalyticsWorkspaceName: module_insights.outputs.logAnalyticsWorkspaceName
    applicationInsightsName: module_insights.outputs.applicationInsightsName
  }
}

/*
  Module to create web app containing the Docker image
  See https://azure.microsoft.com/products/container-apps
  
  The Azure Container app hosts the docker container containing KM web service.
*/
module module_containerApp 'modules/host/container-app.bicep' = {
  name: 'module-containerAppService-${suffix}'
  scope: rg
  params: {
    location: location
    suffix: suffix
    tags: tags
    containerAppsEnvironmentId: module_containerAppsEnvironment.outputs.containerAppsEnvironmentId
    applicationInsightsName: module_insights.outputs.applicationInsightsName
    managedIdentityId: module_managedidentity.outputs.managedIdentityId
    managedIdentityClientId: module_managedidentity.outputs.managedIdentityClientId

    KernelMemoryImageTag: KernelMemoryImageTag

    KernelMemory__ServiceAuthorization__AccessKey1: WebServiceAuthorizationKey1
    KernelMemory__ServiceAuthorization__AccessKey2: WebServiceAuthorizationKey2

    AzureAISearch_Endpoint: 'https://${module_search.outputs.searchName }.search.windows.net'
    AzureBlobs_Account: module_storage.outputs.storageAccountName
    AzureQueues_Account: module_storage.outputs.storageAccountName
    AzureQueues_QueueName: module_storage.outputs.queueName
    AzureOpenAIEmbedding_Deployment: EmbeddingDeploymentName
    AzureOpenAIEmbedding_Endpoint: module_openAi.outputs.endpoint
    AzureOpenAIText_Deployment: InferenceDeploymentName
    AzureOpenAIText_Endpoint: module_openAi.outputs.endpoint
    AzureAIDocIntel_Endpoint: module_docIntel.outputs.endpoint
  }
}

/*
  Module to expose Container App via Azure Application Gateway and Public IP
*/
module module_appGateway 'modules/network/app-gateway.bicep' = {
  name: 'module-appGateway-${suffix}'
  params: {
    location: location
    suffix: suffix
    tags: tags

    defaultDomain: module_containerAppsEnvironment.outputs.containerAppsEnvironmentDomain
    staticIp: module_containerAppsEnvironment.outputs.containerAppsEnvironmentStaticIp
    vnetId: module_vnet.outputs.vnetId

    containerAppFqdn: module_containerApp.outputs.kmServiceFQDN
    subnetId: module_vnet.outputs.appGatewaySubnetId
  }
}

/* 
  Outputs
*/

@description('The public IP of the Kernel Memory service.')
output kmServiceEndpoint string = module_appGateway.outputs.ipAddress

@description('Service Access Key 1.')
output kmServiceAccessKey1 string = module_containerApp.outputs.kmServiceAccessKey1

@description('Service Access Key 2.')
output kmServiceAccessKey2 string = module_containerApp.outputs.kmServiceAccessKey2
