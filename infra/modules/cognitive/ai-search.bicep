param managedIdentityPrincipalId string

param suffix string = uniqueString(resourceGroup().id)

@description('The tags to be assigned to the created resources.')
param tags object

@description('Service name must only contain lowercase letters, digits or dashes, cannot use dash as the first two or last one characters, cannot contain consecutive dashes, and is limited between 2 and 60 characters in length.')
@minLength(2)
@maxLength(60)
param name string = 'km-search-${suffix}'

@allowed([
  'free'
  'basic'
  'standard'
  'standard2'
  'standard3'
  'storage_optimized_l1'
  'storage_optimized_l2'
])
@description('The pricing tier of the search service you want to create (for example, basic or standard).')
param sku string = 'standard'

@description('Replicas distribute search workloads across the service. You need at least two replicas to support high availability of query workloads (not applicable to the free tier).')
@minValue(1)
@maxValue(12)
param replicaCount int = 1

@description('Partitions allow for scaling of document count as well as faster indexing by sharding your index over multiple search units.')
@allowed([
  1
  2
  3
  4
  6
  12
])
param partitionCount int = 1

@description('Applicable only for SKUs set to standard3. You can set this property to enable a single, high density partition that allows up to 1000 indexes, which is much higher than the maximum indexes allowed for any other SKU.')
@allowed([
  'default'
  'highDensity'
])
param hostingMode string = 'default'

@description('Location for all resources.')
param location string = resourceGroup().location

param vnetId string
param privateEndpointSubnetId string

resource search 'Microsoft.Search/searchServices@2023-11-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: sku
  }
  properties: {
    replicaCount: replicaCount
    partitionCount: partitionCount
    hostingMode: hostingMode

    // bohdan Check `disableLocalAuth: true`
    authOptions: {
      aadOrApiKey: {}
    }

    publicNetworkAccess: 'disabled'
  }
}

////////////////////////// Private endpoint

module module_search_pe '../network/private-endpoint.bicep' = {
  name: 'module_search_pe_${suffix}'
  params: {
    suffix: suffix
    location: location
    tags: tags

    serviceName_Used_for_PE: name

    DNSZoneName: 'privatelink.search.windows.net' // https://learn.microsoft.com/en-us/azure/private-link/private-endpoint-dns
    vnetId: vnetId
    privateEndpointSubnetId: privateEndpointSubnetId

    privateLinkServiceId: search.id
    privateLinkServiceConnections_GroupIds: ['searchService'] // https://learn.microsoft.com/en-us/azure/private-link/private-endpoint-overview#private-link-resource
  }
}

////////////////////////// RBAC

// Search Index Data Contributor
resource roleAssignment1 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('Search Index Data Contributor-${suffix}')
  scope: search
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '8ebe5a00-799e-43f5-93ac-243d3dce84a7'
    )
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Search Service Contributor
resource roleAssignment2 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('Search Service Contributor-${suffix}')
  scope: search
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '7ca78c08-252a-4471-8644-bb5ff32d4ba0'
    )
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

////////////////////////// Output

output searchName string = search.name

// output searchObj object = search
// output searchPrivateEndpointObj object = privateEndpoint
