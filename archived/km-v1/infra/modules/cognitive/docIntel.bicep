param suffix string = uniqueString(resourceGroup().id)

param vnetId string
param privateEndpointSubnetId string

param managedIdentityPrincipalId string

metadata description = 'Creates an Azure Document Intelligence (form recognizer) instance.'

param name string
param location string = resourceGroup().location

@description('The tags to be assigned to the created resources.')
param tags object

@description('The custom subdomain name used to access the API. Defaults to the value of the name parameter.')
param customSubDomainName string = name
param kind string = 'FormRecognizer'

@allowed(['Enabled', 'Disabled'])
param publicNetworkAccess string = 'Enabled'
param sku object = {
  name: 'S0'
}

param allowedIpRules array = []
param networkAcls object = empty(allowedIpRules)
  ? {
      defaultAction: 'Allow'
    }
  : {
      ipRules: allowedIpRules
      defaultAction: 'Deny'
    }

resource account 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: name
  location: location
  kind: kind
  tags: tags
  properties: {
    customSubDomainName: customSubDomainName
    publicNetworkAccess: publicNetworkAccess
    networkAcls: networkAcls
    disableLocalAuth: true
  }
  sku: sku
}

////////////////////////// Private endpoint

module module_DocIntel_pe '../network/private-endpoint.bicep' = {
  name: 'module_DocIntel_pe${suffix}'
  params: {
    suffix: suffix
    location: location
    tags: tags

    serviceName_Used_for_PE: name

    DNSZoneName: 'privatelink.cognitiveservices.azure.com' // https://learn.microsoft.com/en-us/azure/private-link/private-endpoint-dns
    vnetId: vnetId
    privateEndpointSubnetId: privateEndpointSubnetId

    privateLinkServiceId: account.id
    privateLinkServiceConnections_GroupIds: ['account'] // https://learn.microsoft.com/en-us/azure/private-link/private-endpoint-overview#private-link-resource
  }
}

////////////////////////// RBAC

// Cognitive Services User
resource roleAssignment1 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('Cognitive Services User-${suffix}')
  scope: account
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      'a97b65f3-24c7-4388-baec-2e87135dc908'
    )
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

////////////////////////// Output

output endpoint string = account.properties.endpoint
