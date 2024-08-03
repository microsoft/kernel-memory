@description('The name of the virtual network')
param vnetName string

@description('Location of the Vnet')
param location string

@description('The tags that will be applied to the VNet')
param tags object

var InfrastructureSubnetName = 'infrastructure-subnet'
var ApplicationGatewaySubnetName = 'app-gateway-subnet'
var PrivateEndpointSubnetName = 'private-endpoint-subnet'

param VirtualNetworkAddressSpace string
param InfrastructureSubnetAddressRange string
param ApplicationGatewaySubnetAddressRange string
param PrivateEndpointSubnetAddressRange string

resource vnet 'Microsoft.Network/virtualNetworks@2023-11-01' = {
  name: vnetName
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        VirtualNetworkAddressSpace
      ]
    }
    subnets: [
      {
        name: InfrastructureSubnetName
        properties: {
          addressPrefix: InfrastructureSubnetAddressRange
          privateEndpointNetworkPolicies: 'Enabled'
          privateLinkServiceNetworkPolicies: 'Disabled'
        }
      }
      {
        name: ApplicationGatewaySubnetName
        properties: {
          addressPrefix: ApplicationGatewaySubnetAddressRange
          privateEndpointNetworkPolicies: 'Disabled'
          privateLinkServiceNetworkPolicies: 'Disabled'
        }
      }
      {
        name: PrivateEndpointSubnetName
        properties: {
          addressPrefix: PrivateEndpointSubnetAddressRange
          privateEndpointNetworkPolicies: 'Enabled'
          privateLinkServiceNetworkPolicies: 'Disabled'
        }
      }
    ]
  }

  resource envInfraSubnet 'subnets' existing = {
    name: InfrastructureSubnetName
  }

  resource appGatewaySubnet 'subnets' existing = {
    name: ApplicationGatewaySubnetName
  }

  resource privateEndpointSubnet 'subnets' existing = {
    name: PrivateEndpointSubnetName
  }
}

output vnetName string = vnet.name
output vnetId string = vnet.id
output envInfraSubnetId string = vnet::envInfraSubnet.id
output appGatewaySubnetId string = vnet::appGatewaySubnet.id
output privateEndpointSubnetId string = vnet::privateEndpointSubnet.id
