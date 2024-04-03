targetScope = 'resourceGroup'

// ------------------
//    PARAMETERS
// ------------------

param salt string = uniqueString(resourceGroup().id)

// @description('The location where the resources will be created.')
param location string = resourceGroup().location

@description('Optional. The tags to be assigned to the created resources.')
param tags object = {}

@description('The name of the Azure Storage Account.')
param storageAccountName string = 'storage${salt}' //'storage${uniqueString(resourceGroup().id)}'

@description('The name of the Container in Azure Storage.')
param storageBlobContainerName string = 'container${salt}'

@description('The name of the Queue in Azure Storage.')
param externalTasksQueueName string = 'queue${salt}'

// ------------------
// RESOURCES
// ------------------

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-09-01' = {
  name: storageAccountName
  tags: tags
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
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

// ------------------
// OUTPUTS
// ------------------

@description('The storage account name.')
output storageAccountName string = storageAccount.name

@description('The storage account name.')
output blobContainerName string = blobContainer.name

@description('The storage account name.')
output queueName string = queue.name
