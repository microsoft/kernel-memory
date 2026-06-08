param suffix string = uniqueString(resourceGroup().id)

param location string = resourceGroup().location

@description('The tags to be assigned to the created resources.')
param tags object

//////////////////////////

param serviceName_Used_for_PE string

param privateEndpointSubnetId string

param privateLinkServiceId string

param privateLinkServiceConnections_GroupIds array

param DNSZoneName string

param vnetId string

//////////////////////////

module module_dns_2 '../network/dns.bicep' = {
  name: 'module-dns-${serviceName_Used_for_PE}-pe'
  params: {
    vnetId: vnetId
    privateDnsZoneName: DNSZoneName
    tags: tags
  }
}

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2024-01-01' = {
  name: '${serviceName_Used_for_PE}-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'private-endpoint-connection'
        properties: {
          privateLinkServiceId: privateLinkServiceId
          groupIds: privateLinkServiceConnections_GroupIds
        }
      }
    ]
  }
}

resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-01-01' = {
  name: 'privateDnsZoneGroup-${suffix}'
  parent: privateEndpoint
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privateDnsZoneGroup-Config-${suffix}'
        properties: {
          privateDnsZoneId: module_dns_2.outputs.dnsZoneId
        }
      }
    ]
  }
}
