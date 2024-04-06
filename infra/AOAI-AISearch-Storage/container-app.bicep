targetScope = 'resourceGroup'

// ------------------
//    PARAMETERS
// ------------------

param salt string = uniqueString(resourceGroup().id)

param location string = resourceGroup().location

param managedIdentityId string

// param subscriptionId string
param kmServiceName string = 'km-service-${salt}'

param containerAppsEnvironmentId string
param appInsightsInstrumentationKey string

// ------------------
// RESOURCES
// ------------------

resource kmService 'Microsoft.App/containerapps@2023-11-02-preview' = {
  name: kmServiceName
  location: location
  tags: {
    CreateContainerApp1Tag: 'CreateContainerApp1TagValue'
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
          name: 'con3app3km3service'
          image: 'docker.io/kernelmemory/service:latest'
          command: []
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'KernelMemory__Service__OpenApiEnabled'
              value: 'true'
              //secretRef: 'appinsights-key'
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
              name: 'KernelMemory__Services__AzureBlobs_Account'
              value: '3333333333333333333333333333333333333333333333333333333333333333'
            }
            {
              name: 'KernelMemory__Services__AzureQueues_Account'
              value: '3333333333333333333333333333333333333333333333333333333333333333'
            }
            {
              name: 'KernelMemory__Services__AzureQueues_QueueName'
              value: '3333333333333333333333333333333333333333333333333333333333333333'
            }
            {
              name: 'KernelMemory__Services__AzureAISearch_Endpoint'
              value: '3333333333333333333333333333333333333333333333333333333333333333'
            }
            {
              name: 'KernelMemory__Services__AzureOpenAIText_Endpoint'
              value: '3333333333333333333'
            }
            {
              name: '11111111111111111111'
              value: '3333333333333333333'
            }
            {
              name: '11111111111111111111'
              value: '3333333333333333333'
            }
            {
              name: '44444444444444444444'
              value: '5555555555555555555'
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
    userAssignedIdentities: managedIdentityId
  }
}

output kmServiceName string = kmService.name
output kmServiceId string = kmService.id
