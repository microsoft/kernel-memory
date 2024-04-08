targetScope = 'resourceGroup'

// ------------------
//    PARAMETERS
// ------------------

param prefix string = uniqueString(resourceGroup().id)

@description('The location where the resources will be created.')
param location string = resourceGroup().location

@description('Optional. The tags to be assigned to the created resources.')
param tags object = {}

@description('The resource Id of the container apps environment.')
param containerAppsEnvironmentId string

@description('The name of the service for the frontend web app service. The name is use as Dapr App ID.')
param frontendWebAppServiceName string

// Container Registry & Image
@description('The name of the container registry.')
param containerRegistryName string

@description('The resource ID of the user assigned managed identity for the container registry to be able to pull images from it.')
param containerRegistryUserAssignedIdentityId string

@description('The image for the frontend web app service.')
param frontendWebAppServiceImage string

@secure()
@description('The Application Insights Instrumentation.')
param appInsightsInstrumentationKey string

@description('The target and dapr port for the frontend web app service.')
param frontendWebAppPortNumber int

// ------------------
// RESOURCES
// ------------------

resource frontendWebAppService 'Microsoft.App/containerApps@2022-06-01-preview' = {
  name: frontendWebAppServiceName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${containerRegistryUserAssignedIdentityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    configuration: {
      activeRevisionsMode: 'single'
      ingress: {
        external: true
        targetPort: frontendWebAppPortNumber
      }
      dapr: {
        enabled: true
        appId: frontendWebAppServiceName
        appProtocol: 'http'
        appPort: frontendWebAppPortNumber
        logLevel: 'info'
        enableApiLogging: true
      }
      secrets: [
        {
          name: 'appinsights-key'
          value: appInsightsInstrumentationKey
        }
      ]
      registries: !empty(containerRegistryName)
        ? [
            {
              server: '${containerRegistryName}.azurecr.io'
              identity: containerRegistryUserAssignedIdentityId
            }
          ]
        : []
    }
    template: {
      containers: [
        {
          name: frontendWebAppServiceName
          image: frontendWebAppServiceImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ApplicationInsights__InstrumentationKey'
              secretRef: 'appinsights-key'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

// ------------------
// OUTPUTS
// ------------------

@description('The name of the container app for the frontend web app service.')
output frontendWebAppServiceContainerAppName string = frontendWebAppService.name

@description('The FQDN of the frontend web app service.')
output frontendWebAppServiceFQDN string = frontendWebAppService.properties.configuration.ingress.fqdn
