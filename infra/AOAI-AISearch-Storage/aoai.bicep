targetScope = 'resourceGroup'

// https://github.com/Azure-Samples/azure-search-openai-demo/blob/main/infra/core/ai/cognitiveservices.bicep

// ------------------
//    PARAMETERS
// ------------------

param salt string = uniqueString(resourceGroup().id)

param location string = resourceGroup().location

param name string = 'km-aoai-${salt}'
param sku string = 'S0'

resource open_ai 'Microsoft.CognitiveServices/accounts@2022-03-01' = {
  name: name
  location: location
  kind: 'OpenAI'
  sku: {
    name: sku
  }
  properties: {
    customSubDomainName: toLower(name)
  }
}

resource deployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = [
  for deployment in deployments: {
    parent: open_ai
    name: deployment.name
    properties: {
      model: deployment.model
      raiPolicyName: contains(deployment, 'raiPolicyName') ? deployment.raiPolicyName : null
    }
    sku: contains(deployment, 'sku')
      ? deployment.sku
      : {
          name: 'Standard'
          capacity: 20
        }
  }
]

output aoaiName string = open_ai.name
