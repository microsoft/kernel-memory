param suffix string = uniqueString(resourceGroup().id)

metadata description = 'Creates an Azure Cognitive Services instance.'
param name string
param location string = resourceGroup().location

@description('The tags to be assigned to the created resources.')
param tags object

@description('The custom subdomain name used to access the API. Defaults to the value of the name parameter.')
param customSubDomainName string = name
param deployments array = []
param kind string = 'OpenAI'

@allowed(['Enabled', 'Disabled'])
param publicNetworkAccess string = 'Enabled'
param sku object = {
  name: 'S0'
}

param allowedIpRules array = []

param vnetId string
param privateEndpointSubnetId string

param managedIdentityPrincipalId string

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
  tags: tags
  kind: kind
  properties: {
    customSubDomainName: customSubDomainName
    publicNetworkAccess: publicNetworkAccess
    networkAcls: networkAcls
    disableLocalAuth: true
  }
  sku: sku
}

@batchSize(1)
resource deployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = [
  for deployment in deployments: {
    parent: account
    name: deployment.name
    properties: {
      model: deployment.model
      raiPolicyName: deployment.?raiPolicyName ?? null
    }
    sku: deployment.?sku ?? {
      name: 'Standard'
      capacity: 1
    }
  }
]

////////////////////////// Private endpoint

module module_openai_pe '../network/private-endpoint.bicep' = {
  name: 'module_openai_pe_${suffix}'
  params: {
    suffix: suffix
    location: location
    tags: tags

    serviceName_Used_for_PE: name

    DNSZoneName: 'privatelink.openai.azure.com' // https://learn.microsoft.com/en-us/azure/private-link/private-endpoint-dns
    vnetId: vnetId
    privateEndpointSubnetId: privateEndpointSubnetId

    privateLinkServiceId: account.id
    privateLinkServiceConnections_GroupIds: ['account'] // https://learn.microsoft.com/en-us/azure/private-link/private-endpoint-overview#private-link-resource
  }
}

////////////////////////// RBAC

// Cognitive Services OpenAI Contributor
resource roleAssignment1 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('Cognitive Services OpenAI Contributor-${suffix}')
  scope: account
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      'a001fd3d-188f-4b5d-821b-7da978bf7442'
    )
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Cognitive Services OpenAI User
resource roleAssignment2 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('Cognitive Services OpenAI User-${suffix}')
  scope: account
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
    )
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

////////////////////////// Output

output endpoint string = account.properties.endpoint
output id string = account.id
output name string = account.name
