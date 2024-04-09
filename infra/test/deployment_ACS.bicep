@description('provide a 2-13 character prefix for all resources.')
param ResourcePrefix string

@description('Azure Cognitive Search Resource')
param AzureCognitiveSearch string = '${ResourcePrefix}-search'

@description('The SKU of the search service you want to create. E.g. free or standard')
@allowed([
  'free'
  'basic'
  'standard'
  'standard2'
  'standard3'
])
param AzureCognitiveSearchSku string = 'standard'

@description('Name of App Service plan')
param HostingPlanName string = '${ResourcePrefix}-plan'

@description('The pricing tier for the App Service plan')
@allowed([
  'F1'
  'D1'
  'B1'
  'B2'
  'B3'
  'S1'
  'S2'
  'S3'
  'P1'
  'P2'
  'P3'
  'P4'
])
param HostingPlanSku string = 'B3'

@description('Name of Storage Account')
param StorageAccountName string = '${ResourcePrefix}str'

@description('Name of Web App')
param WebsiteName string = '${ResourcePrefix}-site'

@description('Name of Function App for Batch document processing')
param FunctionName string = '${ResourcePrefix}-batchfunc'

@description('Name of Application Insights')
param ApplicationInsightsName string = '${ResourcePrefix}-appinsights'

@description('Azure Form Recognizer Name')
param FormRecognizerName string = '${ResourcePrefix}-formrecog'

@description('Azure Translator Name')
param TranslatorName string = '${ResourcePrefix}-translator'

@description('Name of OpenAI Resource')
param OpenAIName string

@description('OpenAI API Key')
@secure()
param OpenAIKey string = ''

@description('OpenAI Engine')
param OpenAIEngine string = 'text-davinci-003'

@description('OpenAI Deployment Type. Text for an Instructions based deployment (text-davinci-003). Chat for a Chat based deployment (gpt-35-turbo or gpt-4-32k or gpt-4).')
param OpenAIDeploymentType string = 'Text'

@description('OpenAI Embeddings Engine for Documents')
param OpenAIEmbeddingsEngineDoc string = 'text-embedding-ada-002'

@description('OpenAI Embeddings Engine for Queries')
param OpenAIEmbeddingsEngineQuery string = 'text-embedding-ada-002'
param newGuid string = newGuid()

var WebAppImageName = 'DOCKER|fruocco/oai-embeddings'
var BlobContainerName = 'documents'
var QueueName = 'doc-processing'
var ClientKey = '${uniqueString(guid(resourceGroup().id, deployment().name))}${newGuid}Tg2%'

resource AzureCognitiveSearch_resource 'Microsoft.Search/searchServices@2015-08-19' = {
  name: AzureCognitiveSearch
  location: resourceGroup().location
  sku: {
    name: AzureCognitiveSearchSku
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
  }
}

resource FormRecognizer 'Microsoft.CognitiveServices/accounts@2022-12-01' = {
  name: FormRecognizerName
  location: resourceGroup().location
  sku: {
    name: 'S0'
  }
  kind: 'FormRecognizer'
  identity: {
    type: 'None'
  }
  properties: {
    networkAcls: {
      defaultAction: 'Allow'
      virtualNetworkRules: []
      ipRules: []
    }
    publicNetworkAccess: 'Enabled'
  }
}

resource Translator 'Microsoft.CognitiveServices/accounts@2022-12-01' = {
  name: TranslatorName
  location: resourceGroup().location
  sku: {
    name: 'S1'
  }
  kind: 'TextTranslation'
  identity: {
    type: 'None'
  }
  properties: {
    networkAcls: {
      defaultAction: 'Allow'
      virtualNetworkRules: []
      ipRules: []
    }
    publicNetworkAccess: 'Enabled'
  }
}

resource HostingPlan 'Microsoft.Web/serverfarms@2020-06-01' = {
  name: HostingPlanName
  location: resourceGroup().location
  sku: {
    name: HostingPlanSku
  }
  properties: {
    name: HostingPlanName
    reserved: true
  }
  kind: 'linux'
}

resource Website 'Microsoft.Web/sites@2020-06-01' = {
  name: WebsiteName
  location: resourceGroup().location
  properties: {
    serverFarmId: HostingPlanName
    siteConfig: {
      linuxFxVersion: WebAppImageName
    }
  }
  dependsOn: [
    HostingPlan
  ]
}

resource StorageAccount 'Microsoft.Storage/storageAccounts@2021-08-01' = {
  name: StorageAccountName
  location: resourceGroup().location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_GRS'
  }
}

resource StorageAccountName_default_BlobContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-08-01' = {
  name: '${StorageAccountName}/default/${BlobContainerName}'
  properties: {
    publicAccess: 'None'
  }
  dependsOn: [
    StorageAccount
  ]
}

resource StorageAccountName_default 'Microsoft.Storage/storageAccounts/queueServices@2022-09-01' = {
  parent: StorageAccount
  name: 'default'
  properties: {
    cors: {
      corsRules: []
    }
  }
}

resource StorageAccountName_default_doc_processing 'Microsoft.Storage/storageAccounts/queueServices/queues@2022-09-01' = {
  parent: StorageAccountName_default
  name: 'doc-processing'
  properties: {
    metadata: {}
  }
  dependsOn: [

    StorageAccount
  ]
}

resource StorageAccountName_default_doc_processing_poison 'Microsoft.Storage/storageAccounts/queueServices/queues@2022-09-01' = {
  parent: StorageAccountName_default
  name: 'doc-processing-poison'
  properties: {
    metadata: {}
  }
  dependsOn: [

    StorageAccount
  ]
}

resource ApplicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: ApplicationInsightsName
  location: resourceGroup().location
  tags: {
    'hidden-link:${resourceId('Microsoft.Web/sites', ApplicationInsightsName)}': 'Resource'
  }
  properties: {
    Application_Type: 'web'
  }
  kind: 'web'
}

resource Function 'Microsoft.Web/sites@2018-11-01' = {
  name: FunctionName
  kind: 'functionapp,linux'
  location: resourceGroup().location
  tags: {}
  properties: {
    name: FunctionName
    siteConfig: {
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'false'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: reference(ApplicationInsights.id, '2015-05-01').InstrumentationKey
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${StorageAccountName};AccountKey=${listKeys(StorageAccount.id, '2019-06-01').keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'OPENAI_ENGINE'
          value: OpenAIEngine
        }
        {
          name: 'OPENAI_DEPLOYMENT_TYPE'
          value: OpenAIDeploymentType
        }
        {
          name: 'OPENAI_EMBEDDINGS_ENGINE_DOC'
          value: OpenAIEmbeddingsEngineDoc
        }
        {
          name: 'OPENAI_EMBEDDINGS_ENGINE_QUERY'
          value: OpenAIEmbeddingsEngineQuery
        }
        {
          name: 'OPENAI_API_BASE'
          value: 'https://${OpenAIName}.openai.azure.com/'
        }
        {
          name: 'OPENAI_API_KEY'
          value: OpenAIKey
        }
        {
          name: 'BLOB_ACCOUNT_NAME'
          value: StorageAccountName
        }
        {
          name: 'BLOB_ACCOUNT_KEY'
          value: listKeys(StorageAccount.id, '2019-06-01').keys[0].value
        }
        {
          name: 'BLOB_CONTAINER_NAME'
          value: BlobContainerName
        }
        {
          name: 'FORM_RECOGNIZER_ENDPOINT'
          value: 'https://${resourceGroup().location}.api.cognitive.microsoft.com/'
        }
        {
          name: 'FORM_RECOGNIZER_KEY'
          value: listKeys('Microsoft.CognitiveServices/accounts/${FormRecognizerName}', '2023-05-01').key1
        }
        {
          name: 'VECTOR_STORE_TYPE'
          value: 'AzureSearch'
        }
        {
          name: 'AZURE_SEARCH_SERVICE_NAME'
          value: 'https://${AzureCognitiveSearch}.search.windows.net'
        }
        {
          name: 'AZURE_SEARCH_ADMIN_KEY'
          value: listAdminKeys('Microsoft.Search/searchServices/${AzureCognitiveSearch}', '2021-04-01-preview').primaryKey
        }
        {
          name: 'TRANSLATE_ENDPOINT'
          value: 'https://api.cognitive.microsofttranslator.com/'
        }
        {
          name: 'TRANSLATE_KEY'
          value: listKeys('Microsoft.CognitiveServices/accounts/${TranslatorName}', '2023-05-01').key1
        }
        {
          name: 'TRANSLATE_REGION'
          value: resourceGroup().location
        }
        {
          name: 'QUEUE_NAME'
          value: QueueName
        }
      ]
      cors: {
        allowedOrigins: [
          'https://portal.azure.com'
        ]
      }
      use32BitWorkerProcess: false
      linuxFxVersion: 'DOCKER|fruocco/oai-batch:latest'
      appCommandLine: ''
      alwaysOn: true
    }
    serverFarmId: HostingPlan.id
    clientAffinityEnabled: false
    virtualNetworkSubnetId: null
    httpsOnly: true
  }
}

resource FunctionName_default_clientKey 'Microsoft.Web/sites/host/functionKeys@2018-11-01' = {
  name: '${FunctionName}/default/clientKey'
  properties: {
    name: 'ClientKey'
    value: ClientKey
  }
  dependsOn: [
    Function
    WaitFunctionDeploymentSection
  ]
}

resource WebsiteName_appsettings 'Microsoft.Web/sites/config@2021-03-01' = {
  parent: Website
  name: 'appsettings'
  kind: 'string'
  properties: {
    APPINSIGHTS_INSTRUMENTATIONKEY: reference(ApplicationInsights.id, '2015-05-01').InstrumentationKey
    OPENAI_ENGINE: OpenAIEngine
    OPENAI_DEPLOYMENT_TYPE: OpenAIDeploymentType
    OPENAI_EMBEDDINGS_ENGINE_DOC: OpenAIEmbeddingsEngineDoc
    OPENAI_EMBEDDINGS_ENGINE_QUERY: OpenAIEmbeddingsEngineQuery
    VECTOR_STORE_TYPE: 'AzureSearch'
    AZURE_SEARCH_SERVICE_NAME: 'https://${AzureCognitiveSearch}.search.windows.net'
    AZURE_SEARCH_ADMIN_KEY: listAdminKeys('Microsoft.Search/searchServices/${AzureCognitiveSearch}', '2021-04-01-preview').primaryKey
    OPENAI_API_BASE: 'https://${OpenAIName}.openai.azure.com/'
    OPENAI_API_KEY: OpenAIKey
    BLOB_ACCOUNT_NAME: StorageAccountName
    BLOB_ACCOUNT_KEY: listkeys(StorageAccount.id, '2015-05-01-preview').key1
    BLOB_CONTAINER_NAME: BlobContainerName
    FORM_RECOGNIZER_ENDPOINT: 'https://${resourceGroup().location}.api.cognitive.microsoft.com/'
    FORM_RECOGNIZER_KEY: listKeys('Microsoft.CognitiveServices/accounts/${FormRecognizerName}', '2023-05-01').key1
    TRANSLATE_ENDPOINT: 'https://api.cognitive.microsofttranslator.com/'
    TRANSLATE_KEY: listKeys('Microsoft.CognitiveServices/accounts/${TranslatorName}', '2023-05-01').key1
    TRANSLATE_REGION: resourceGroup().location
    CONVERT_ADD_EMBEDDINGS_URL: 'https://${FunctionName}.azurewebsites.net/api/BatchStartProcessing?code=${ClientKey}'
  }
}

resource WaitFunctionDeploymentSection 'Microsoft.Resources/deploymentScripts@2020-10-01' = {
  kind: 'AzurePowerShell'
  name: 'WaitFunctionDeploymentSection'
  location: resourceGroup().location
  properties: {
    azPowerShellVersion: '3.0'
    scriptContent: 'start-sleep -Seconds 300'
    cleanupPreference: 'Always'
    retentionInterval: 'PT1H'
  }
  dependsOn: [
    Function
  ]
}