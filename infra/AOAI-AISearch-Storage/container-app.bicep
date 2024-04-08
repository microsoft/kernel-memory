targetScope = 'resourceGroup'

// ------------------
//    PARAMETERS
// ------------------

param suffix string = uniqueString(resourceGroup().id)

param location string = resourceGroup().location

param managedIdentityId string
param managedIdentityClientId string

// param subscriptionId string
param kmServiceName string = 'km-service-${suffix}'

param containerAppsEnvironmentId string
param appInsightsInstrumentationKey string
param applicationInsightsConnectionString string

param AzureBlobs_Account string
param AzureQueues_Account string
param AzureQueues_QueueName string
param AzureAISearch_Endpoint string
param AzureOpenAIText_Endpoint string
param AzureOpenAIText_Deployment string
param AzureOpenAIEmbedding_Endpoint string
param AzureOpenAIEmbedding_Deployment string

// ------------------
// RESOURCES
// ------------------

resource kmService 'Microsoft.App/containerapps@2023-11-02-preview' = {
  name: kmServiceName
  location: location
  tags: {
    // CreateContainerApp1Tag: 'CreateContainerApp1TagValue'
  }
  // kind: 'containerapps'
  properties: {
    environmentId: containerAppsEnvironmentId
    configuration: {
      secrets: [
        {
          name: 'appinsights-key'
          value: appInsightsInstrumentationKey
        }
      ]
      registries: []
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        transport: 'Auto'
        allowInsecure: false
        targetPort: 9001
        stickySessions: {
          affinity: 'none'
        }
        additionalPortMappings: []
      }
    }

    template: {
      containers: [
        {
          name: 'kernelmemory-service'
          image: 'docker.io/bc123456/service:latest'
          command: []
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
              //secretRef: 'appinsights-key'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: applicationInsightsConnectionString
            }

            {
              name: 'AZURE_CLIENT_ID'
              value: managedIdentityClientId
            }
            {
              name: 'KernelMemory__Service__OpenApiEnabled'
              value: 'true'
            }
            {
              name: 'KernelMemory__ContentStorageType'
              value: 'AzureBlobs'
            }
            {
              name: 'KernelMemory__TextGeneratorType'
              value: 'AzureOpenAIText'
            }
            {
              name: 'KernelMemory__DefaultIndexName'
              value: 'default'
            }
            {
              name: 'KernelMemory__ServiceAuthorization__Enabled'
              value: 'true'
            }
            {
              name: 'KernelMemory__ServiceAuthorization__AuthenticationType'
              value: 'APIKey'
            }
            {
              name: 'KernelMemory__ServiceAuthorization__HttpHeaderName'
              value: 'Authorization'
            }
            {
              name: 'KernelMemory__ServiceAuthorization__AccessKey1'
              value: 'ApiKey1ValueApiKey1ValueApiKey1Value'
            }
            {
              name: 'KernelMemory__ServiceAuthorization__AccessKey2'
              value: 'ApiKey2ValueApiKey2ValueApiKey2Value'
            }
            {
              name: 'KernelMemory__DataIngestion__DistributedOrchestration__QueueType'
              value: 'AzureQueues'
            }
            {
              name: 'KernelMemory__DataIngestion__EmbeddingGeneratorTypes'
              value: '["AzureOpenAIEmbedding"]'
            }
            {
              name: 'KernelMemory__DataIngestion__MemoryDbTypes'
              value: '["AzureAISearch"]'
            }
            {
              name: 'KernelMemory__Retrieval__EmbeddingGeneratorType'
              value: 'AzureOpenAIEmbedding'
            }
            {
              name: 'KernelMemory__Retrieval__MemoryDbType'
              value: 'AzureAISearch'
            }
            {
              name: 'KernelMemory__Services__AzureBlobs__Account'
              value: AzureBlobs_Account
            }
            {
              name: 'KernelMemory__Services__AzureQueues__Account'
              value: AzureQueues_Account
            }
            {
              name: 'KernelMemory__Services__AzureQueues__QueueName'
              value: AzureQueues_QueueName
            }
            {
              name: 'KernelMemory__Services__AzureAISearch__Endpoint'
              value: AzureAISearch_Endpoint
            }
            {
              name: 'KernelMemory__Services__AzureOpenAIText__Endpoint'
              value: AzureOpenAIText_Endpoint
            }
            {
              name: 'KernelMemory__Services__AzureOpenAIText__Deployment'
              value: AzureOpenAIText_Deployment
            }
            {
              name: 'KernelMemory__Services__AzureOpenAIEmbedding__Endpoint'
              value: AzureOpenAIEmbedding_Endpoint
            }
            {
              name: 'KernelMemory__Services__AzureOpenAIEmbedding__Deployment'
              value: AzureOpenAIEmbedding_Deployment
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
    // workloadProfileName: 'Consumption'
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
}

output kmServiceName string = kmService.name
output kmServiceId string = kmService.id
