@description('Azure region for all resources')
param location string = 'japaneast'

@description('Short prefix used to name resources')
param prefix string = 'atoaPoc'

var tags = {
  project: 'atoa-poc'
  env: 'dev'
  app: 'migration-fortune'
  managedBy: 'bicep'
}

module storageModule './storage.bicep' = {
  name: 'storageDeployment'
  params: {
    location: location
    prefix: prefix
    tags: tags
  }
}

module functionsModule './functions.bicep' = {
  name: 'functionsDeployment'
  params: {
    location: location
    prefix: prefix
    storageAccountName: storageModule.outputs.storageAccountName
    tags: tags
  }
}
