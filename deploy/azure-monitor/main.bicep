@description('Name of the Direct data collection rule.')
param dcrName string = 'dpi-nugetreport'

@description('Azure region for the data collection rule. Use the same region as the Log Analytics workspace.')
param location string

@description('Resource ID of the destination Log Analytics workspace.')
param workspaceResourceId string

@description('Optional object ID of the sender service principal. Leave empty to skip the role assignment.')
param senderPrincipalId string = ''

@description('Input stream name used by dpi nuget report. Distinct from the table output stream Custom-<table>.')
param streamName string = 'Custom-NuGetReport'

@description('Destination custom table. Migrate NuGetReport_CL from classic before deploying this DCR.')
param tableName string = 'NuGetReport_CL'

var outputStream = 'Custom-${tableName}'
var monitoringMetricsPublisherRoleId = '3913510d-42f4-4e42-8a64-420c390055eb'
// DPI JSON uses unsuffixed names; a migrated Data Collector table keeps *_s/*_t/*_g columns.
var transformKql = '''
source
| extend TimeGenerated = iif(isempty(tostring(timestamp)), now(), todatetime(timestamp))
| extend sessionId_g = toguid(sessionId)
| extend timestamp_t = todatetime(timestamp)
| extend targetFramework_s = targetFramework
| extend buildVersion_s = buildVersion
| extend buildNo_s = buildNo
| extend platformFamily_s = platformFamily
| extend buildSCM_s = buildSCM
| extend buildProvider_s = buildProvider
| extend source_s = ['source']
| extend sourceType_s = sourceType
| extend packageId_s = packageId
| extend version_s = version
| project TimeGenerated, Computer, timestamp_t, sessionId_g, targetFramework_s, buildVersion_s, buildNo_s, platformFamily_s, buildSCM_s, buildProvider_s, source_s, sourceType_s, packageId_s, version_s
'''

resource dcr 'Microsoft.Insights/dataCollectionRules@2023-03-11' = {
  name: dcrName
  location: location
  kind: 'Direct'
  properties: {
    description: 'DPI NuGetReport Logs Ingestion API rule targeting the existing NuGetReport_CL table.'
    destinations: {
      logAnalytics: [
        {
          name: 'dpiWorkspace'
          workspaceResourceId: workspaceResourceId
        }
      ]
    }
    dataFlows: [
      {
        streams: [
          streamName
        ]
        destinations: [
          'dpiWorkspace'
        ]
        transformKql: transformKql
        outputStream: outputStream
      }
    ]
    streamDeclarations: {
      '${streamName}': {
        columns: [
          { name: 'timestamp', type: 'datetime' }
          { name: 'sessionId', type: 'string' }
          { name: 'buildProvider', type: 'string' }
          { name: 'platformFamily', type: 'string' }
          { name: 'buildNo', type: 'string' }
          { name: 'buildSCM', type: 'string' }
          { name: 'buildVersion', type: 'string' }
          { name: 'sourceType', type: 'string' }
          { name: 'source', type: 'string' }
          { name: 'targetFramework', type: 'string' }
          { name: 'packageId', type: 'string' }
          { name: 'version', type: 'string' }
          { name: 'Computer', type: 'string' }
        ]
      }
    }
  }
}

resource senderRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(senderPrincipalId)) {
  name: guid(dcr.id, senderPrincipalId, monitoringMetricsPublisherRoleId)
  scope: dcr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', monitoringMetricsPublisherRoleId)
    principalId: senderPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output endpoint string = dcr.properties.endpoints.logsIngestion
output ruleId string = dcr.properties.immutableId
output stream string = streamName
output dataCollectionRuleId string = dcr.id
