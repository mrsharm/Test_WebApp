// main.bicep
// Deploys an Azure App Service Plan and a Web App
// Note: Free tier (F1) does not support auto-scaling and is limited to 1 instance

param location string = resourceGroup().location
param appServicePlanName string = 'cpu-app20250714124552Plan'
param webAppName string = 'test_webapp'

// App Service Plan with explicit scaling configuration
resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'F1'
    tier: 'Free'
    capacity: 1 // Free tier is always 1 instance
  }
  kind: 'app'
  properties: {
    // Free tier limitations:
    // - Cannot scale below 1 instance
    // - Cannot scale above 1 instance
    // - No auto-scaling support
    // - Manual scaling not supported
  }
}

resource webApp 'Microsoft.Web/sites@2022-03-01' = {
  name: webAppName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      // Application settings for scaling awareness
      appSettings: [
        {
          name: 'SCALING_TIER'
          value: 'Free'
        }
        {
          name: 'MIN_INSTANCES'
          value: '1'
        }
        {
          name: 'MAX_INSTANCES'  
          value: '1'
        }
        {
          name: 'SCALING_SUPPORTED'
          value: 'false'
        }
      ]
    }
  }
}

// Output scaling information for operational awareness
output webAppUrl string = 'https://${webApp.name}.azurewebsites.net'
output scalingInfo object = {
  tier: 'Free'
  sku: 'F1'
  minInstances: 1
  maxInstances: 1
  scalingSupported: false
  autoScalingSupported: false
  currentInstances: 1
  message: 'Scale-down operations will fail as app is already at minimum instance count (1) for Free tier'
}
