@description('Suffix to create unique resource names; 4-6 characters. Default is a random 6 characters.')
@minLength(4)
@maxLength(6)
param suffix string = substring(newGuid(), 0, 6)

@description('''
gpt-35-turbo-16k deployment model\'s Tokens-Per-Minute (TPM) capacity, measured in thousands.
The default capacity is 30,000 TPM. 
For model limits specific to your region, refer to the documentation at https://learn.microsoft.com/azure/ai-services/openai/concepts/models#standard-deployment-model-quota.
''')
@minValue(1)
@maxValue(40)
param chatGptDeploymentCapacity int = 30

@description('''
text-embedding-ada-002 deployment model\'s Tokens-Per-Minute (TPM) capacity, measured in thousands.
The default capacity is 30,000 TPM.
For model limits specific to your region, refer to the documentation at https://learn.microsoft.com/azure/ai-services/openai/concepts/models#standard-deployment-model-quota.
''')
@minValue(1)
@maxValue(40)
param embeddingDeploymentCapacity int = 30

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

var rg = resourceGroup()

var location = resourceGroup().location

var chatGpt = {
  modelName: 'gpt-35-turbo-16k'
  deploymentName: 'chat'
  deploymentVersion: '0613'
  deploymentCapacity: chatGptDeploymentCapacity
}

var embedding = {
  modelName: 'text-embedding-ada-002'
  deploymentName: 'embedding'
  deploymentVersion: '2'
  deploymentCapacity: embeddingDeploymentCapacity
}

var openAiDeployments = [
  {
    name: chatGpt.deploymentName
    model: {
      format: 'OpenAI'
      name: chatGpt.modelName
      version: chatGpt.deploymentVersion
    }
    sku: {
      name: 'Standard'
      capacity: chatGpt.deploymentCapacity
    }
  }
  {
    name: embedding.deploymentName
    model: {
      format: 'OpenAI'
      name: embedding.modelName
      version: embedding.deploymentVersion
    }
    sku: {
      name: 'Standard'
      capacity: embedding.deploymentCapacity
    }
  }
]

/*
  Module to create a Managed Identity.
  See https://learn.microsoft.com/entra/identity/managed-identities-azure-resources/overview
  
  The managed identity is the main code-to-services and service-to-service authentication mechanism.
*/
module managedidentity 'modules/managed-identity.bicep' = {
  name: 'managedidentity-${suffix}'
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
module storage 'modules/storage.bicep' = {
  name: 'storage-${suffix}'
  scope: rg
  params: {
    location: location
    suffix: suffix
    managedIdentityPrincipalId: managedidentity.outputs.managedIdentityPrincipalId
  }
}

/*
  Module to create a Azure AI Search service
  See https://azure.microsoft.com/products/ai-services/ai-search
  
  Azure AI Search is used to store document chunks and LLM embeddings, and to search
  for relevant data when searching memories and asking questions.
*/
module search 'modules/ai-search.bicep' = {
  name: 'search-${suffix}'
  scope: rg
  params: {
    location: location
    name: 'km-search-${suffix}'
    suffix: suffix
    managedIdentityPrincipalId: managedidentity.outputs.managedIdentityPrincipalId
  }
}

/*
  Module to create a Azure OpenAI service
  See https://azure.microsoft.com/products/ai-services/openai-service
      and https://github.com/Azure-Samples/azure-search-openai-demo/blob/main/infra/main.bicep for more details
  
  Azure OpenAI is used to generate text embeddings, and to generate text from memories (answers and summaries)
*/
module openAi 'modules/cognitive-services-openAI.bicep' = {
  name: 'openai-${suffix}'
  scope: rg
  params: {
    suffix: suffix
    managedIdentityPrincipalId: managedidentity.outputs.managedIdentityPrincipalId
    name: 'km-openai-${suffix}'
    location: location
    sku: {
      name: 'S0'
    }
    deployments: openAiDeployments
  }
}

/*
  Module to create a Azure Document Intelligence service
  See https://azure.microsoft.com/products/ai-services/ai-document-intelligence
  Azure Document Intelligence is used to extract text from images
*/
module docIntel 'modules/cognitive-services-docIntel.bicep' = {
  name: 'docIntel-${suffix}'
  scope: rg
  params: {
    suffix: suffix
    managedIdentityPrincipalId: managedidentity.outputs.managedIdentityPrincipalId
    name: 'km-docIntel-${suffix}'
    location: location
    sku: {
      name: 'S0'
    }
  }
}

/* 
  Module to create an Azure Container Apps environment and a container app
  See https://learn.microsoft.com/en-us/azure/container-apps/environment
      and https://azure.github.io/aca-dotnet-workshop/aca/10-aca-iac-bicep/iac-bicep/#2-define-an-azure-container-apps-environment for more samples
*/
module containerAppsEnvironment 'modules/container-apps-environment.bicep' = {
  name: 'containerAppsEnvironment-${suffix}'
  scope: rg
  params: {
    location: location
    suffix: suffix
  }
}

/*
  Module to create web app containing the Docker image
  See https://azure.microsoft.com/products/container-apps
  
  The Azure Container app hosts the docker container containing KM web service.
*/
module containerAppService 'modules/container-app.bicep' = {
  name: 'containerAppService-${suffix}'
  scope: rg
  params: {
    location: location
    suffix: suffix
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.containerAppsEnvironmentId
    appInsightsInstrumentationKey: containerAppsEnvironment.outputs.applicationInsightsInstrumentationKey
    applicationInsightsConnectionString: containerAppsEnvironment.outputs.applicationInsightsConnectionString
    managedIdentityId: managedidentity.outputs.managedIdentityId
    managedIdentityClientId: managedidentity.outputs.managedIdentityClientId

    KernelMemory__ServiceAuthorization__AccessKey1: WebServiceAuthorizationKey1
    KernelMemory__ServiceAuthorization__AccessKey2: WebServiceAuthorizationKey2

    AzureAISearch_Endpoint: 'https://${search.outputs.searchName }.search.windows.net'
    AzureBlobs_Account: storage.outputs.storageAccountName
    AzureQueues_Account: storage.outputs.storageAccountName
    AzureQueues_QueueName: storage.outputs.queueName
    AzureOpenAIEmbedding_Deployment: embedding.deploymentName
    AzureOpenAIEmbedding_Endpoint: openAi.outputs.endpoint
    AzureOpenAIText_Deployment: chatGpt.deploymentName
    AzureOpenAIText_Endpoint: openAi.outputs.endpoint
    AzureAIDocIntel_Endpoint: docIntel.outputs.endpoint
  }
}

/* 
  Outputs
*/

@description('The FQDN of the frontend web app service.')
output kmServiceEndpoint string = containerAppService.outputs.kmServiceFQDN

@description('Service Access Key 1.')
output kmServiceAccessKey1 string = containerAppService.outputs.kmServiceAccessKey1

@description('Service Access Key 2.')
output kmServiceAccessKey2 string = containerAppService.outputs.kmServiceAccessKey2
