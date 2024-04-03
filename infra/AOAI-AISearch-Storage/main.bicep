targetScope = 'subscription'

param location string
param salt string

resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'akm${salt}'
  location: location
}

// //------- Storage Account Module

module storage 'storage.bicep' = {
  name: 'storage-${salt}'
  scope: rg
  params: {
    location: location
    salt: salt
  }
}

var storageOutput = {
  storageAccountName: storage.outputs.storageAccountName
  queueName: storage.outputs.queueName
}


//------- Search Module

module search 'search.bicep' = {
  name: 'search-${salt}'
  scope: rg
  params: {
    location: location
    name: 'search-${salt}'
    salt: '${salt}'
  }
}

var searchOutput = {
  searchName: search.outputs.searchName
  searchIndexName: search.outputs.searchObj
  adminKey: search.outputs.adminKey
  queryKey: search.outputs.queryKey
}

// //------- Container Apps Environment Module

module containerAppsEnvironment 'container-apps-environment.bicep' = {
  name: 'containerAppsEnvironment-${salt}'
  scope: rg
  params: {
    location: location
    salt: salt
  }
}

var containerAppsEnvironmentOutput = {
  containerAppsEnvironmentId: containerAppsEnvironment.outputs.containerAppsEnvironmentId
  containerAppsEnvironmentName: containerAppsEnvironment.outputs.containerAppsEnvironmentName
  logAnalyticsWorkspaceName: containerAppsEnvironment.outputs.logAnalyticsWorkspaceName
  applicationInsightsName: containerAppsEnvironment.outputs.applicationInsightsName
  applicationInsightsInstrumentationKey: containerAppsEnvironment.outputs.applicationInsightsInstrumentationKey
}

// //------- Container Apps Environment Module

module containerAppService 'service.bicep' = {
  name: 'service-${salt}'
  scope: rg
  params: {
    location: location
    salt: salt
    containerAppsEnvironmentId: containerAppsEnvironmentOutput.containerAppsEnvironmentId
    appInsightsInstrumentationKey: containerAppsEnvironmentOutput.applicationInsightsInstrumentationKey
  }
}

var containerAppServiceOutput = {
  kmServiceId: containerAppService.outputs.kmServiceId
  kmServiceName: containerAppService.outputs.kmServiceName
}

// //------- Output

output storageOutput                  object = storageOutput
output searchOutput                   object = searchOutput
output containerAppsEnvironmentOutput object = containerAppsEnvironmentOutput
output serviceOutput                  object = containerAppServiceOutput
