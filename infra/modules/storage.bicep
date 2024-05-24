targetScope = 'resourceGroup'

param suffix string = uniqueString(resourceGroup().id)

param location string = resourceGroup().location

@description('Optional. The tags to be assigned to the created resources.')
param tags object = {}

@description('The name of the Azure Storage Account.')
param storageAccountName string = 'kmstorage${suffix}' //'storage${uniqueString(resourceGroup().id)}'

@description('The name of the Container in Azure Storage.')
param storageBlobContainerName string = 'smemory'

@description('The name of the Queue in Azure Storage.')
param externalTasksQueueName string = 'km-queue-${suffix}'

param managedIdentityPrincipalId string

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-09-01' = {
  name: storageAccountName
  tags: tags
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
  }
}

// Storage Queue Data Contributor
resource roleAssignment1 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('Storage Queue Data Contributor-${suffix}')
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
    )
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Storage Blob Data Contributor
resource roleAssignment2 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('Storage Blob Data Contributor-${suffix}')
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
    )
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Storage Blob Data Owner
resource roleAssignment22 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('Storage Blob Data Owner-${suffix}')
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
    )
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Storage Queue Data Message Sender
resource roleAssignment3 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('Storage Queue Data Message Sender-${suffix}')
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      'c6a89b2d-59bc-44d0-9896-0f6e12d7b80a'
    )
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Storage Queue Data Message Processor
resource roleAssignment4 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('// Storage Queue Data Message Processor-${suffix}')
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '8a0f0c08-91a1-4084-bc3d-661d67233fed'
    )
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource storageBlobService 'Microsoft.Storage/storageAccounts/blobServices@2021-09-01' = {
  name: 'default'
  parent: storageAccount
}

resource blobContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-09-01' = {
  parent: storageBlobService
  name: storageBlobContainerName
}

resource storageQueuesService 'Microsoft.Storage/storageAccounts/queueServices@2021-09-01' = {
  name: 'default'
  parent: storageAccount
}

resource queue 'Microsoft.Storage/storageAccounts/queueServices/queues@2021-09-01' = {
  name: externalTasksQueueName
  parent: storageQueuesService
}

@description('The storage account name.')
output storageAccountName string = storageAccount.name

@description('The storage account name.')
output blobContainerName string = blobContainer.name

@description('The storage account name.')
output queueName string = queue.name
