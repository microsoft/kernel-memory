param salt string = uniqueString(resourceGroup().id)

param managedIdentityId string

@description('Service name must only contain lowercase letters, digits or dashes, cannot use dash as the first two or last one characters, cannot contain consecutive dashes, and is limited between 2 and 60 characters in length.')
@minLength(2)
@maxLength(60)
param name string

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

resource search 'Microsoft.Search/searchServices@2020-08-01' = {
  name: name
  location: location
  sku: {
    name: sku
  }
  properties: {
    replicaCount: replicaCount
    partitionCount: partitionCount
    hostingMode: hostingMode
  }
}

resource scriptWait 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: '${search.name}-wait'
  location: location
  kind: 'AzureCLI'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    azCliVersion: '2.52.0'
    // scriptContent: 'az search service wait --service-name ${search.name} --resource-group ${resourceGroup().name} --created'
    environmentVariables: [
      {
        name: 'gr'
        value: '${resourceGroup().name}'
      }
      {
        name: 'SearchName'
        value: '${search.name}'
      }
    ]
    //arguments: ' -gr ${resourceGroup().name} -SearchName ${search.name}'
    scriptContent: '''
    queryKey=$((az search query-key list -g $gr --service-name "$SearchName" --query "[0].key") | xargs)
    adminKey=$((az search admin-key show -g $gr --service-name "$SearchName" --query "primaryKey") | xargs)

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
indexJson=$(cat <<EOF
{ 
  "name": "default", 
  "fields": [ 
    { "name": "HotelId", "type": "Edm.String", "key": true, "retrievable": true, "searchable": true, "filterable": true }, 
    { "name": "HotelName", "type": "Edm.String", "retrievable": true, "searchable": true, "filterable": false, "sortable": true, "facetable": false }, 
    { "name": "Description", "type": "Edm.String", "retrievable": true, "searchable": true, "filterable": false, "sortable": false, "facetable": false, "analyzer": "en.microsoft" }, 
    { "name": "Description_fr", "type": "Edm.String", "retrievable": true, "searchable": true, "filterable": false, "sortable": false, "facetable": false, "analyzer": "fr.microsoft" }, 
    { "name": "Address", "type": "Edm.ComplexType",  
      "fields": [ 
          { "name": "StreetAddress", "type": "Edm.String", "retrievable": true, "filterable": false, "sortable": false, "facetable": false, "searchable": true }, 
          { "name": "City", "type": "Edm.String", "retrievable": true, "searchable": true, "filterable": true, "sortable": true, "facetable": true }, 
          { "name": "StateProvince", "type": "Edm.String", "retrievable": true, "searchable": true, "filterable": true, "sortable": true, "facetable": true } 
        ] 
    } 
  ] 
}
EOF
)
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////

    apicall=$(curl -H "Content-Type: application/json" \
      -H "api-key: $adminKey" \
      -d "$indexJson" \
      -X POST \
      https://$SearchName.search.windows.net/indexes?api-version=2020-06-30)
    
    echo "{\"queryKey\": \"$queryKey\", \"adminKey\": \"$adminKey\"}" > $AZ_SCRIPTS_OUTPUT_PATH
  '''
    // timeout: 'PT15M'
    cleanupPreference: 'OnSuccess'
    retentionInterval: 'PT1H'
  }
  dependsOn: [
    search
  ]
}

// output adminKey string = scriptWait.properties.outputs.adminKey
// output queryKey string = scriptWait.properties.outputs.queryKey

output searchName string = search.name
