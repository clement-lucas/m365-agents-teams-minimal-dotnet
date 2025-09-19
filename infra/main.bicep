param location string = resourceGroup().location
param namePrefix string = 'm365agents'

var storageName = toLower(replace('${namePrefix}${uniqueString(resourceGroup().id)}','-',''))
var appInsightsName = '${namePrefix}-appi'
var planName = '${namePrefix}-plan'
var functionName = '${namePrefix}-func'
var cosmosName = '${namePrefix}-${uniqueString(resourceGroup().id)}'

resource stg 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
}

resource appi 'Microsoft.Insights/components@2022-04-01' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: { Application_Type: 'web' }
}

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  sku: { name: 'Y1', tier: 'Dynamic' }
}

resource func 'Microsoft.Web/sites@2023-12-01' = {
  name: functionName
  location: location
  kind: 'functionapp,linux'
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      appSettings: [
        { name: 'AzureWebJobsStorage', value: stg.listKeys().keys[0].value }
        { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
        { name: 'WEBSITE_RUN_FROM_PACKAGE', value: '1' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appi.properties.ConnectionString }
      ]
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
    }
  }
  identity: { type: 'SystemAssigned' }
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-02-15' = {
  name: cosmosName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: { defaultConsistencyLevel: 'Session' }
    locations: [{
      locationName: location
      failoverPriority: 0
      isZoneRedundant: false
    }]
    publicNetworkAccess: 'Enabled'
  }
}

resource db 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-02-15' = {
  name: '${cosmos.name}/botdb'
  properties: {
    resource: { id: 'botdb' }
    options: { throughput: 400 }
  }
}

resource container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-02-15' = {
  name: '${cosmos.name}/botdb/conversationRefs'
  properties: {
    resource: {
      id: 'conversationRefs'
      partitionKey: { paths: ['/partitionKey'], kind: 'Hash' }
    }
  }
}

output functionAppName string = functionName
output cosmosEndpoint string = cosmos.properties.documentEndpoint
