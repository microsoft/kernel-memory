// targetScope = 'subscription'

var rg = resourceGroup()

var location = resourceGroup().location

@description('Suffix to create uniqute resource names. 4-6 symbols. Default is random 6 symbols.')
@minLength(4)
@maxLength(6)
param suffix string = substring(newGuid(), 0, 6)

// resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
//   name: 'km-rg-${suffix}'
//   location: location
// }

/////////////////////////////////////////////////////////////////////////////////////////

// //------- Managed Identity

module managedidentity 'managedidentity.bicep' = {
  name: 'managedidentity-${suffix}'
  scope: rg
  params: {
    location: location
    suffix: suffix
  }
}

// //------- Storage Account Module

module storage 'storage.bicep' = {
  name: 'storage-${suffix}'
  scope: rg
  params: {
    location: location
    suffix: suffix
  }
}

var storageOutput = {
  storageAccountName: storage.outputs.storageAccountName
  queueName: storage.outputs.queueName
}

//------- Search Module

module search 'search.bicep' = {
  name: 'search-${suffix}'
  scope: rg
  params: {
    location: location
    name: 'km-search-${suffix}'
    suffix: suffix
    managedIdentityId: managedidentity.outputs.managedIdentityId
  }
}

var searchOutput = {
  searchName: search.outputs.searchName
}

//------- Search Module

//https://github.com/Azure-Samples/azure-search-openai-demo/blob/main/infra/main.bicep
// @allowed(['azure', 'openai', 'azure_custom'])
var openAiHost = 'azure'

var openAiSkuName = 'S0'

var chatGptModelName = ''
var chatGptDeploymentName = ''
var chatGptDeploymentVersion = ''
var chatGptDeploymentCapacity = 1 // Bohdan
var chatGpt = {
  modelName: !empty(chatGptModelName)
    ? chatGptModelName
    : startsWith(openAiHost, 'azure') ? 'gpt-35-turbo' : 'gpt-3.5-turbo'
  deploymentName: !empty(chatGptDeploymentName) ? chatGptDeploymentName : 'chat'
  deploymentVersion: !empty(chatGptDeploymentVersion) ? chatGptDeploymentVersion : '0613'
  deploymentCapacity: chatGptDeploymentCapacity != 0 ? chatGptDeploymentCapacity : 29
}

var embeddingModelName = ''
var embeddingDeploymentName = ''
var embeddingDeploymentVersion = ''
var embeddingDeploymentCapacity = 1 // Bohdan
var embeddingDimensions = 0
var embedding = {
  modelName: !empty(embeddingModelName) ? embeddingModelName : 'text-embedding-ada-002'
  deploymentName: !empty(embeddingDeploymentName) ? embeddingDeploymentName : 'embedding'
  deploymentVersion: !empty(embeddingDeploymentVersion) ? embeddingDeploymentVersion : '2'
  deploymentCapacity: embeddingDeploymentCapacity != 0 ? embeddingDeploymentCapacity : 29
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

var openAiServiceName = 'km-openai-${suffix}'

module openAi 'cognitiveservices.bicep' = {
  name: 'openai-${suffix}'
  scope: rg
  params: {
    name: openAiServiceName
    location: location
    // ags: []
    sku: {
      name: openAiSkuName
    }
    deployments: openAiDeployments
  }
}

var aoaiOutput = {
  aoaiEndpoint: openAi.outputs.endpoint
  aoaiId: openAi.outputs.id
  aoaiName: openAi.outputs.name
}

//------- Container Apps Environment Module

module containerAppsEnvironment 'container-apps-environment.bicep' = {
  name: 'containerAppsEnvironment-${suffix}'
  scope: rg
  params: {
    location: location
    suffix: suffix
  }
}

var containerAppsEnvironmentOutput = {
  containerAppsEnvironmentId: containerAppsEnvironment.outputs.containerAppsEnvironmentId
  containerAppsEnvironmentName: containerAppsEnvironment.outputs.containerAppsEnvironmentName
  logAnalyticsWorkspaceName: containerAppsEnvironment.outputs.logAnalyticsWorkspaceName
  applicationInsightsName: containerAppsEnvironment.outputs.applicationInsightsName
  applicationInsightsInstrumentationKey: containerAppsEnvironment.outputs.applicationInsightsInstrumentationKey
  applicationInsightsConnectionString: containerAppsEnvironment.outputs.applicationInsightsConnectionString
}

// //------- Container Apps Module

module containerAppService 'container-app.bicep' = {
  name: 'containerAppService-${suffix}'
  scope: rg
  params: {
    location: location
    suffix: suffix
    containerAppsEnvironmentId: containerAppsEnvironmentOutput.containerAppsEnvironmentId
    appInsightsInstrumentationKey: containerAppsEnvironmentOutput.applicationInsightsInstrumentationKey
    applicationInsightsConnectionString: containerAppsEnvironmentOutput.applicationInsightsConnectionString
    managedIdentityId: managedidentity.outputs.managedIdentityId
    managedIdentityClientId: managedidentity.outputs.managedIdentityClientId

    AzureAISearch_Endpoint: 'https://${searchOutput.searchName}.search.windows.net'
    AzureBlobs_Account: storageOutput.storageAccountName
    AzureQueues_Account: storageOutput.storageAccountName
    AzureQueues_QueueName: storageOutput.queueName
    AzureOpenAIEmbedding_Deployment: embedding.deploymentName
    AzureOpenAIEmbedding_Endpoint: openAi.outputs.endpoint
    AzureOpenAIText_Deployment: chatGpt.deploymentName
    AzureOpenAIText_Endpoint: openAi.outputs.endpoint
  }
}

var containerAppServiceOutput = {
  kmServiceId: containerAppService.outputs.kmServiceId
  kmServiceName: containerAppService.outputs.kmServiceName
}

// //------- Output

output storageOutput object = storageOutput
output searchOutput object = searchOutput
output containerAppsEnvironmentOutput object = containerAppsEnvironmentOutput
output serviceOutput object = containerAppServiceOutput
output aoaiOutput object = aoaiOutput
