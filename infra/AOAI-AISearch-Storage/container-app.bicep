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
              name: 'ApplicationInsights__InstrumentationKey'
              secretRef: 'appinsights-key'
            }
            {
              name: 'envVar1Name'
              value: 'envVar1Value'
            }
            {
              name: 'envVar2Name'
              value: 'envVar2Value'
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
