var rg = resourceGroup()
var location = resourceGroup().location

@description('Suffix to create uniqute resource names. 4-6 symbols. Default is random 6 symbols.')
@minLength(4)
@maxLength(6)
param suffix string = substring(newGuid(), 0, 6)

// //az deployment sub create -f main.bicep --location=$Location --parameters location=$Location -c
// targetScope = 'subscription'
// param location string
// resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
//   name: 'km-rg-${suffix}'
//   location: location
// }

module managedidentity 'teamplates/identity.bicep' = {
  name: 'managedidentity-${suffix}'
  scope: rg
  params: {
    location: location
    suffix: suffix
  }
}

module storage 'teamplates/storage.bicep' = {
  name: 'storage-${suffix}'
  scope: rg
  params: {
    location: location
    suffix: suffix
    managedIdentityPrincipalId: managedidentity.outputs.managedIdentityPrincipalId
  }
}

module search 'teamplates/search.bicep' = {
  name: 'search-${suffix}'
  scope: rg
  params: {
    location: location
    name: 'km-search-${suffix}'
    suffix: suffix
    managedIdentityPrincipalId: managedidentity.outputs.managedIdentityPrincipalId
  }
}

@description('ChatGpt Deployment Capacity in thousands. Default is 1.')
@minValue(1)
@maxValue(40)
param chatGptDeploymentCapacity int = 30

var chatGpt = {
  modelName: 'gpt-35-turbo-16k'
  deploymentName: 'chat'
  deploymentVersion: '0613'
  deploymentCapacity: chatGptDeploymentCapacity
}

@description('Embedding Deployment Capacity in thousands. Default is 1.')
@minValue(1)
@maxValue(40)
param embeddingDeploymentCapacity int = 30
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

module openAi 'teamplates/cognitiveservices.bicep' = {
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

module containerAppsEnvironment 'teamplates/container-apps-environment.bicep' = {
  name: 'containerAppsEnvironment-${suffix}'
  scope: rg
  params: {
    location: location
    suffix: suffix
  }
}

module containerAppService 'teamplates/container-app.bicep' = {
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

@description('The FQDN of the frontend web app service.')
output kmServiceEndpoint string = containerAppService.outputs.kmServiceFQDN

@description('Service Access Key 1.')
output kmServiceAccessKey1 string = containerAppService.outputs.kmServiceAccessKey1

@description('Service Access Key 2.')
output kmServiceAccessKey2 string = containerAppService.outputs.kmServiceAccessKey2
