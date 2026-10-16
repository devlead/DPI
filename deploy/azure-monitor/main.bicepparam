using 'main.bicep'

param dcrName = 'dpi-nugetreport'
param location = '<workspace-region>'
param workspaceResourceId = '/subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.OperationalInsights/workspaces/<workspace-name>'
param senderPrincipalId = ''
param streamName = 'Custom-NuGetReport'
param tableName = 'NuGetReport_CL'
