var rg = resourceGroup()
var location = resourceGroup().location

@description('Suffix to create unique resource names; 4-6 characters. Default is a random 6 characters.')
@minLength(4)
@maxLength(6)
param suffix string = substring(newGuid(), 0, 6)

@description('gpt-35-turbo-16k deployment model\'s Tokens-Per-Minute (TPM) capacity, measured in thousands. The default capacity is 30,000 TPM. For model limits specific to your region, refer to the documentation at https://learn.microsoft.com/azure/ai-services/openai/concepts/models#standard-deployment-model-quota.')
@minValue(1)
@maxValue(40)
param chatGptDeploymentCapacity int = 30

@description('text-embedding-ada-002 deployment model\'s Tokens-Per-Minute (TPM) capacity, measured in thousands. The default capacity is 30,000 TPM. For model limits specific to your region, refer to the documentation at https://learn.microsoft.com/azure/ai-services/openai/concepts/models#standard-deployment-model-quota.')
@minValue(1)
@maxValue(40)
param embeddingDeploymentCapacity int = 30

/*
  Module to create a managed identity
*/

module managedidentity 'templates/identity.bicep' = {
  name: 'managedidentity-${suffix}'
  scope: rg
  params: {
    location: location
    suffix: suffix
  }
}

/* 
  Module to create a storage account
*/
module storage 'templates/storage.bicep' = {
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
*/
module search 'templates/ai-search.bicep' = {
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
  refer to https://github.com/Azure-Samples/azure-search-openai-demo/blob/main/infra/main.bicep for more details
*/

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

module openAi 'templates/cognitive-services.bicep' = {
  name: 'openai-${suffix}'
  scope: rg
  params: {
    suffix: suffix
    managedIdentityPrincipalId: managedidentity.outputs.managedIdentityPrincipalId
    name: 'km-openai-${suffix}'
    location: location
    // ags: []
    sku: {
      name: 'S0'
    }
    deployments: openAiDeployments
  }
}

/* 
  Module to create an Azure Container Apps environment and a container app
  refer to https://azure.github.io/aca-dotnet-workshop/aca/10-aca-iac-bicep/iac-bicep/#2-define-an-azure-container-apps-environment for more samples
*/
module containerAppsEnvironment 'templates/container-apps-environment.bicep' = {
  name: 'containerAppsEnvironment-${suffix}'
  scope: rg
  params: {
    location: location
    suffix: suffix
  }
}

module containerAppService 'templates/container-app.bicep' = {
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

    AzureAISearch_Endpoint: 'https://${search.outputs.searchName }.search.windows.net'
    AzureBlobs_Account: storage.outputs.storageAccountName
    AzureQueues_Account: storage.outputs.storageAccountName
    AzureQueues_QueueName: storage.outputs.queueName
    AzureOpenAIEmbedding_Deployment: embedding.deploymentName
    AzureOpenAIEmbedding_Endpoint: openAi.outputs.endpoint
    AzureOpenAIText_Deployment: chatGpt.deploymentName
    AzureOpenAIText_Endpoint: openAi.outputs.endpoint
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
