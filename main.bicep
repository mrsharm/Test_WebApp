// main.bicep
// Deploys an Azure App Service Plan and a Web App

param location string = resourceGroup().location
param appServicePlanName string = 'cpu-app20250714124552Plan'
param webAppName string = 'test_webapp'

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'F1'
    tier: 'Free'
  }
  kind: 'app'
}

resource webApp 'Microsoft.Web/sites@2022-03-01' = {
  name: webAppName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
  }
}

output webAppUrl string = 'https://${webApp.name}.azurewebsites.net'
