# DPI

[![NuGet](https://img.shields.io/nuget/v/dpi.svg)](https://www.nuget.org/packages/dpi)

Dependency Inventory .NET Tool - Inventories dependencies and reports to Azure Monitor Logs

You can get an introduction to the tool, in the blog post: [Introducing DPI - A DevOps tool to inspect dependencies and report to Azure Log Analytics](https://www.devlead.se/posts/2021/2021-03-20-introducing-dpi)

## Obtain

`dotnet tool install -g dpi`

## Commands

Use `-h` / `--help` to get the current list of available commands and options.

```bash
dpi --help
```

### nuget

The NuGet branch of commands have recursively from given path inventories packages present in

* csproj (project assets if restored)
* packages.config
* .NET Tool manifests
* Cake build script files
* C# file-based apps (`*.cs` files that start with `#!/usr/bin/env dotnet`, inventorying `#:sdk` and `#:package`; omitted `#:sdk` versions are taken from `msbuild-sdks` in `global.json`)

#### analyze

**nuget analyze** command inventories and outputs result to console or file.

```bash
# Output as table (default)
dpi nuget --output table analyze

# Output as JSON
dpi nuget --output json analyze

# Output as Markdown
dpi nuget --output markdown analyze

# Save output to file
dpi nuget --output json --file dependencies.json analyze
```

#### report

**nuget report** analyzes and reports the result to Azure Monitor Logs through the [Logs Ingestion API](https://learn.microsoft.com/en-us/azure/azure-monitor/logs/logs-ingestion-api-overview). Authenticate with `DefaultAzureCredential` (Azure CLI, managed identity, GitHub OIDC, or Azure Pipelines workload identity). Do not pass a client secret to DPI.

```bash
export NuGetReportSettings_Endpoint=https://<dcr-or-dce-ingestion-host>
export NuGetReportSettings_RuleId=dcr-<immutable-id>
# Optional; defaults to Custom-NuGetReport (DCR input stream, not the table name)
export NuGetReportSettings_Stream=Custom-NuGetReport

dpi nuget --output table report
dpi nuget --output json report
dpi nuget --output markdown report
dpi nuget --output json --file report.json report
```

CLI equivalents: `--endpoint`, `--rule-id`, and `--stream`.

This release still accepts the deprecated HTTP Data Collector API pair `--workspace` / `--sharedkey` (or `NuGetReportSettings_WorkspaceId` / `NuGetReportSettings_SharedKey`) and writes a warning. Do not mix those settings with Logs Ingestion settings. Shared-key ingestion will be removed in a later release.

##### Migrate an existing `NuGetReport_CL` table

The HTTP Data Collector API is [unsupported](https://learn.microsoft.com/en-us/azure/azure-monitor/logs/custom-logs-migrate). Existing DPI customers should keep the current `NuGetReport_CL` table and history.

1. Confirm the table is still classic (`schema.tableSubType` is `Classic`). `table show` uses `--name`; snapshot queries should use Data Collector suffixes (`sessionId_g`, `packageId_s`, `timestamp_t`).
2. Export or snapshot current queries, workbooks, and a sample of recent rows.
3. Migrate the table in place. This Azure operation cannot be rolled back. After migrate, `tableSubType` is `DataCollectionRuleBased` and the suffixed columns stay.

Set `$owner` and `$repo` to your GitHub origin (`devlead` / `DPI` here). Azure names in this walkthrough use a lowercase repo slug (`devlead-dpi`).

Bash:

```bash
owner=devlead
repo=DPI
resourceGroup=dpi
workspaceName="${owner}-${repo,,}"
dcrName=dpi-nugetreport
```

PowerShell:

```powershell
$owner = 'devlead'
$repo = 'DPI'
$resourceGroup = 'dpi'
$workspaceName = "$owner-$($repo.ToLowerInvariant())"
$dcrName = 'dpi-nugetreport'
```

Bash:

```bash
az monitor log-analytics workspace table migrate \
  --resource-group "$resourceGroup" \
  --workspace-name "$workspaceName" \
  --table-name NuGetReport_CL
```

PowerShell:

```powershell
az monitor log-analytics workspace table migrate `
  --resource-group $resourceGroup `
  --workspace-name $workspaceName `
  --table-name NuGetReport_CL
```

4. Deploy the Direct data collection rule in [`deploy/azure-monitor/main.bicep`](deploy/azure-monitor/main.bicep). Fill in [`deploy/azure-monitor/main.bicepparam`](deploy/azure-monitor/main.bicepparam) first. Use the same Azure region as the workspace.

Bash:

```bash
az deployment group create \
  --resource-group "$resourceGroup" \
  --template-file deploy/azure-monitor/main.bicep \
  --parameters deploy/azure-monitor/main.bicepparam
```

PowerShell:

```powershell
az deployment group create `
  --resource-group $resourceGroup `
  --template-file deploy/azure-monitor/main.bicep `
  --parameters deploy/azure-monitor/main.bicepparam
```

The deployment outputs `endpoint`, `ruleId`, and `stream` (the DCR input stream, default `Custom-NuGetReport`). The table stream remains `Custom-NuGetReport_CL`. Grant the runtime identity **Monitoring Metrics Publisher** on that data collection rule. A role on the Log Analytics workspace does not authorize ingestion. Role assignments can take up to 30 minutes; earlier calls return HTTP 403.

DPI posts to the input stream `Custom-NuGetReport`. The DCR transform maps those JSON names (`sessionId`, `packageId`, `timestamp`) onto the migrated table columns and `outputStream` `Custom-NuGetReport_CL`. The project-path field is `extend source_s = ['source']` so it is not the KQL `source` operator. After a canary `dpi nuget report`, query `NuGetReport_CL` by `sessionId_g` and compare row counts, `timestamp_t`, `Computer`, and `packageId_s`. Rows can take a few minutes to appear. For a local canary with `az login`, assign **Monitoring Metrics Publisher** to the signed-in user as well as any service principal. Remove workspace-key secrets only after that check succeeds. Application fallback is pinning the previous DPI version with the shared-key pair; the table migration itself cannot be reversed. When you add DPI fields later, update the table schema and the DCR stream together.

##### Create the Entra sender app

The signed-in administrator needs User Access Administrator or Owner on the data collection rule. Confirm `az account show` is the intended subscription. If `az ad app list --display-name dpi-logs-ingestion` already returns an app, reuse that `appId` instead of creating a second one. Create the app with no client secret.

Bash:

```bash
appId=$(az ad app create --display-name "dpi-logs-ingestion" --query appId --output tsv)
spId=$(az ad sp create --id "$appId" --query id --output tsv)
tenantId=$(az account show --query tenantId --output tsv)
subscriptionId=$(az account show --query id --output tsv)
az ad app credential list --id "$appId"
```

PowerShell:

```powershell
$appId = (az ad app create --display-name "dpi-logs-ingestion" --query appId --output tsv).Trim()
$spId = (az ad sp create --id $appId --query id --output tsv).Trim()
$tenantId = (az account show --query tenantId --output tsv).Trim()
$subscriptionId = (az account show --query id --output tsv).Trim()
az ad app credential list --id $appId
```

`az ad app credential list` should be empty. Grant **Monitoring Metrics Publisher** on the data collection rule. Reuse `$dcrName` and `$resourceGroup` from the Bicep deploy. Pass `--subscription` so the assignment is not treated as a tenant-level request.

Bash:

```bash
dcrId=$(az monitor data-collection rule show \
  --name "$dcrName" \
  --resource-group "$resourceGroup" \
  --subscription "$subscriptionId" \
  --query id --output tsv)

az role assignment create \
  --assignee-object-id "$spId" \
  --assignee-principal-type ServicePrincipal \
  --role "Monitoring Metrics Publisher" \
  --scope "$dcrId" \
  --subscription "$subscriptionId"

userId=$(az ad signed-in-user show --query id --output tsv)
az role assignment create \
  --assignee-object-id "$userId" \
  --assignee-principal-type User \
  --role "Monitoring Metrics Publisher" \
  --scope "$dcrId" \
  --subscription "$subscriptionId"
```

PowerShell:

```powershell
$dcrId = (az monitor data-collection rule show `
  --name $dcrName `
  --resource-group $resourceGroup `
  --subscription $subscriptionId `
  --query id --output tsv).Trim()

az role assignment create `
  --assignee-object-id $spId `
  --assignee-principal-type ServicePrincipal `
  --role "Monitoring Metrics Publisher" `
  --scope $dcrId `
  --subscription $subscriptionId

$userId = (az ad signed-in-user show --query id --output tsv).Trim()
az role assignment create `
  --assignee-object-id $userId `
  --assignee-principal-type User `
  --role "Monitoring Metrics Publisher" `
  --scope $dcrId `
  --subscription $subscriptionId
```

Keep workspace rights with the deployment administrator: Log Analytics Contributor migrates `NuGetReport_CL`, and Monitoring Contributor creates the DCR.

##### GitHub Actions OIDC

Add one federated credential per trusted branch (`main` and `develop`). Do not federate `pull_request` (untrusted forks). Subjects use `repo:$owner/$repo`; credential names use the lowercase repo slug. Store `AZURE_CLIENT_ID=$appId`, `AZURE_TENANT_ID=$tenantId`, and `AZURE_SUBSCRIPTION_ID=$subscriptionId`.

Bash:

```bash
repoSlug=${repo,,}
for branch in main develop; do
  az ad app federated-credential create --id "$appId" --parameters "{
    \"name\": \"github-${owner}-${repoSlug}-${branch}\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${owner}/${repo}:ref:refs/heads/${branch}\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }"
done
```

PowerShell:

```powershell
$repoSlug = $repo.ToLowerInvariant()
@(
  @{ name = "github-$owner-$repoSlug-main"; subject = "repo:${owner}/${repo}:ref:refs/heads/main" },
  @{ name = "github-$owner-$repoSlug-develop"; subject = "repo:${owner}/${repo}:ref:refs/heads/develop" }
) | ForEach-Object {
  @{
    name = $_.name
    issuer = "https://token.actions.githubusercontent.com"
    subject = $_.subject
    audiences = @("api://AzureADTokenExchange")
  } | ConvertTo-Json | Set-Content "$($_.name).json"
  az ad app federated-credential create --id $appId --parameters "$($_.name).json"
}
Remove-Item "github-$owner-$repoSlug-main.json", "github-$owner-$repoSlug-develop.json"
```

```yaml
permissions:
  id-token: write
  contents: read

- name: Azure login
  if: ${{ (github.event_name == 'push' || github.event_name == 'workflow_dispatch') && (github.ref == 'refs/heads/main' || github.ref == 'refs/heads/develop') }}
  uses: azure/login@v3
  with:
    client-id: ${{ secrets.AZURE_CLIENT_ID }}
    tenant-id: ${{ secrets.AZURE_TENANT_ID }}
    subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

Store the OIDC app and Logs Ingestion settings as Actions secrets. Delete the Data Collector pair if it is still present; DPI rejects mixed modes.

```powershell
"$appId" | gh secret set AZURE_CLIENT_ID
"$tenantId" | gh secret set AZURE_TENANT_ID
"$subscriptionId" | gh secret set AZURE_SUBSCRIPTION_ID
"$endpoint" | gh secret set NUGETREPORTSETTINGS_ENDPOINT
"$ruleId" | gh secret set NUGETREPORTSETTINGS_RULEID
gh secret delete NUGETREPORTSETTINGS_STREAM
gh secret delete NUGETREPORTSETTINGS_SHAREDKEY
gh secret delete NUGETREPORTSETTINGS_WORKSPACEID
```

`$endpoint` and `$ruleId` are the Bicep deployment outputs. CI uses the tool default stream (`Custom-NuGetReport`); set `NUGETREPORTSETTINGS_STREAM` only to override it. `azure/login` signs in Azure CLI; DPI then uses that session through `DefaultAzureCredential`. Pass Logs Ingestion secrets into the workflow only on push or `workflow_dispatch` of `main` or `develop`, not on pull requests. Do not pass the Data Collector workspace/shared-key pair alongside them.

##### Azure Pipelines workload identity

On Azure public cloud, create a manual Azure Resource Manager service connection for the single-tenant app, then require the returned issuer to be the Microsoft Entra issuer `https://login.microsoftonline.com/<tenantId>/v2.0` with issuer type `EntraID`. The Azure DevOps CLI needs a one-time `AZURE_DEVOPS_EXT_PAT` with permission to manage service connections; that token is not a DPI runtime secret. See [Automate Azure Resource Manager workload identity service connections](https://learn.microsoft.com/en-us/azure/devops/pipelines/release/automate-service-connections?view=azure-devops).

Bash:

```bash
az extension add --name azure-devops
az devops configure --defaults organization="https://dev.azure.com/$org" project="$project"
projectId=$(az devops project show --project "$project" --query id --output tsv)
```

PowerShell:

```powershell
az extension add --name azure-devops
az devops configure --defaults organization="https://dev.azure.com/$org" project="$project"
$projectId = (az devops project show --project $project --query id --output tsv).Trim()
```

Create `dpi-service-connection.json` with `"creationMode": "Manual"` and `"scheme": "WorkloadIdentityFederation"`, then:

Bash:

```bash
endpointParameters=$(az devops service-endpoint create \
  --service-endpoint-configuration ./dpi-service-connection.json \
  --query authorization.parameters \
  --output json)
issuer=$(jq -r '.workloadIdentityFederationIssuer' <<<"$endpointParameters")
issuerType=$(jq -r '.workloadIdentityFederationIssuerType' <<<"$endpointParameters")
subject=$(jq -r '.workloadIdentityFederationSubject' <<<"$endpointParameters")
expectedIssuer="https://login.microsoftonline.com/${tenantId}/v2.0"

if [[ "$issuerType" != "EntraID" || "$issuer" != "$expectedIssuer" ]]; then
  echo "Expected Microsoft Entra issuer ${expectedIssuer} with issuer type EntraID." >&2
  exit 1
fi

az ad app federated-credential create --id "$appId" --parameters "$(jq -n \
  --arg issuer "$issuer" \
  --arg subject "$subject" \
  '{name:"ado-dpi-logs-ingestion", issuer:$issuer, subject:$subject, audiences:["api://AzureADTokenExchange"]}')"
```

PowerShell:

```powershell
$endpointParameters = az devops service-endpoint create `
  --service-endpoint-configuration ./dpi-service-connection.json `
  --query authorization.parameters `
  --output json | ConvertFrom-Json
$expectedIssuer = "https://login.microsoftonline.com/$tenantId/v2.0"

if ($endpointParameters.workloadIdentityFederationIssuerType -ne "EntraID" -or
    $endpointParameters.workloadIdentityFederationIssuer -ne $expectedIssuer) {
  throw "Expected Microsoft Entra issuer $expectedIssuer with issuer type EntraID."
}

@{
  name = "ado-dpi-logs-ingestion"
  issuer = $endpointParameters.workloadIdentityFederationIssuer
  subject = $endpointParameters.workloadIdentityFederationSubject
  audiences = @("api://AzureADTokenExchange")
} | ConvertTo-Json | Set-Content ado-dpi-logs-ingestion.json

az ad app federated-credential create --id $appId --parameters ado-dpi-logs-ingestion.json
```

Run DPI inside `AzureCLI@2` with `azureSubscription: dpi-logs-ingestion` and the same endpoint, rule ID, and stream variables used by GitHub Actions.

Example `dpi-service-connection.json`:

```json
{
  "name": "dpi-logs-ingestion",
  "type": "AzureRM",
  "url": "https://management.azure.com/",
  "isShared": false,
  "isReady": true,
  "authorization": {
    "scheme": "WorkloadIdentityFederation",
    "parameters": {
      "tenantid": "<tenantId>",
      "serviceprincipalid": "<appId>"
    }
  },
  "data": {
    "subscriptionId": "<subscriptionId>",
    "subscriptionName": "<subscriptionName>",
    "environment": "AzureCloud",
    "scopeLevel": "Subscription",
    "creationMode": "Manual"
  },
  "serviceEndpointProjectReferences": [
    {
      "projectReference": { "id": "<projectId>", "name": "<project>" },
      "name": "dpi-logs-ingestion"
    }
  ]
}
```

Example pipeline. `AzureCLI@2` signs in with the service connection so `dnx dpi` can use `DefaultAzureCredential`:

```yaml
trigger:
  - main

pool:
  vmImage: ubuntu-latest

steps:
  - task: UseDotNet@2
    inputs:
      packageType: sdk
      version: 10.x

  - task: AzureCLI@2
    displayName: Report NuGet inventory to Azure Monitor Logs
    env:
      NuGetReportSettings_Endpoint: $(DPI_LOGS_ENDPOINT)
      NuGetReportSettings_RuleId: $(DPI_LOGS_RULE_ID)
    inputs:
      azureSubscription: dpi-logs-ingestion
      scriptType: bash
      scriptLocation: inlineScript
      inlineScript: |
        dnx dpi --yes -- nuget "$(Build.SourcesDirectory)" --output table --buildversion "$(Build.BuildNumber)" report
```
