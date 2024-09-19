param suffix string = uniqueString(resourceGroup().id)

@description('The location where the App Gateway will be deployed')
param location string

@description('The tags that will be applied to the App Gateway')
param tags object

param staticIp string
param defaultDomain string
param vnetId string

@description('The subnet ID that will be used for the App Gateway configuration')
param subnetId string

@description('The FQDN of the Container App')
param containerAppFqdn string

////////////////////////////////////////////////////////

var appGatewayName = 'km-appG-${suffix}'

var ipAddressName = 'km-appG-pip-${suffix}'

////////////////////////////////////////////////////////  Private DNS Zone

resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: defaultDomain
  location: 'global'
  tags: tags
}

resource starRecordSet 'Microsoft.Network/privateDnsZones/A@2020-06-01' = {
  name: '*'
  parent: privateDnsZone
  properties: {
    ttl: 3600
    aRecords: [
      {
        ipv4Address: staticIp
      }
    ]
  }
}

resource atRecordSet 'Microsoft.Network/privateDnsZones/A@2020-06-01' = {
  name: '@'
  parent: privateDnsZone
  properties: {
    ttl: 3600
    aRecords: [
      {
        ipv4Address: staticIp
      }
    ]
  }
}

resource privateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  name: 'pdns-link'
  parent: privateDnsZone
  tags: tags
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnetId
    }
  }
}

////////////////////////////////////////////////////////  PiP and App Gateway

resource publicIp 'Microsoft.Network/publicIPAddresses@2023-11-01' = {
  name: ipAddressName
  location: location
  sku: {
    name: 'Standard'
  }
  zones: [
    '1'
  ]
  properties: {
    publicIPAddressVersion: 'IPv4'
    publicIPAllocationMethod: 'Static'
  }
}

resource appGateway 'Microsoft.Network/applicationGateways@2023-11-01' = {
  name: appGatewayName
  location: location
  tags: tags
  zones: [
    '1'
  ]
  properties: {
    sku: {
      tier: 'Standard_v2'
      capacity: 1
      name: 'Standard_v2'
    }
    gatewayIPConfigurations: [
      {
        name: 'appgateway-subnet'
        properties: {
          subnet: {
            id: subnetId
          }
        }
      }
    ]
    frontendIPConfigurations: [
      {
        name: 'my-frontend'
        properties: {
          publicIPAddress: {
            id: publicIp.id
          }
        }
      }
    ]
    frontendPorts: [
      {
        name: 'port_80'
        properties: {
          port: 80
        }
      }
    ]
    backendAddressPools: [
      {
        name: 'my-agw-backend-pool'
        properties: {
          backendAddresses: [
            {
              fqdn: containerAppFqdn
            }
          ]
        }
      }
    ]
    probes: [
      {
        name: 'health-http'
        properties: {
          protocol: 'Http'
          path: '/health'
          interval: 30
          timeout: 30
          unhealthyThreshold: 3
          pickHostNameFromBackendHttpSettings: true
          minServers: 0
          match: {}
        }
      }
      {
        name: 'health-https'
        properties: {
          protocol: 'Https'
          path: '/health'
          interval: 30
          timeout: 30
          unhealthyThreshold: 3
          pickHostNameFromBackendHttpSettings: true
          minServers: 0
          match: {}
        }
      }
    ]
    backendHttpSettingsCollection: [
      {
        name: 'backend-setting-https'
        properties: {
          protocol: 'Https'
          port: 443
          cookieBasedAffinity: 'Disabled'
          requestTimeout: 20
          pickHostNameFromBackendAddress: true
          probe: {
            id: resourceId('Microsoft.Network/applicationGateways/probes', appGatewayName, 'health-https')
          }
        }
      }
      {
        name: 'backend-setting-http'
        properties: {
          protocol: 'Http'
          port: 80
          cookieBasedAffinity: 'Disabled'
          requestTimeout: 20
          pickHostNameFromBackendAddress: true
          probe: {
            id: resourceId('Microsoft.Network/applicationGateways/probes', appGatewayName, 'health-http')
          }
        }
      }
    ]
    httpListeners: [
      {
        name: 'my-agw-listener'
        properties: {
          frontendIPConfiguration: {
            id: resourceId(
              'Microsoft.Network/applicationGateways/frontendIPConfigurations',
              appGatewayName,
              'my-frontend'
            )
          }
          frontendPort: {
            id: resourceId('Microsoft.Network/applicationGateways/frontendPorts', appGatewayName, 'port_80')
          }
          protocol: 'Http'
        }
      }
    ]
    requestRoutingRules: [
      {
        name: 'my-agw-routing-rule'
        properties: {
          priority: 1
          ruleType: 'Basic'
          httpListener: {
            id: resourceId('Microsoft.Network/applicationGateways/httpListeners', appGatewayName, 'my-agw-listener')
          }
          backendAddressPool: {
            id: resourceId(
              'Microsoft.Network/applicationGateways/backendAddressPools',
              appGatewayName,
              'my-agw-backend-pool'
            )
          }
          backendHttpSettings: {
            id: resourceId(
              'Microsoft.Network/applicationGateways/backendHttpSettingsCollection',
              appGatewayName,
              'backend-setting-https'
            )
          }
        }
      }
    ]
    enableHttp2: true
  }
}

@description('Public IP')
output ipAddress string = publicIp.properties.ipAddress
