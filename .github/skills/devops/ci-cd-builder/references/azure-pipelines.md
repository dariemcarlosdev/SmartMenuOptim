# Azure Pipelines Reference

## YAML Pipeline Structure

Azure Pipelines uses a hierarchical structure: **Stages → Jobs → Steps**.

```yaml
trigger:
  branches:
    include: [main, develop]
  paths:
    exclude: [docs/*, '*.md']

pr:
  branches:
    include: [main]

pool:
  vmImage: 'ubuntu-latest'

variables:
  - group: 'escrow-app-settings'
  - name: buildConfiguration
    value: 'Release'
  - name: dotnetVersion
    value: '10.0.x'

stages:
  - stage: Build
    jobs:
      - job: BuildJob
        steps:
          - task: UseDotNet@2
            inputs:
              version: $(dotnetVersion)
          - script: dotnet build -c $(buildConfiguration)
```

## Template References and Parameters

Extract reusable pipeline logic into templates:

```yaml
# templates/dotnet-build.yml
parameters:
  - name: configuration
    type: string
    default: 'Release'
  - name: projects
    type: string
    default: '**/*.csproj'

steps:
  - task: UseDotNet@2
    displayName: 'Install .NET SDK'
    inputs:
      version: $(dotnetVersion)

  - task: DotNetCoreCLI@2
    displayName: 'Restore'
    inputs:
      command: restore
      projects: ${{ parameters.projects }}

  - task: DotNetCoreCLI@2
    displayName: 'Build'
    inputs:
      command: build
      projects: ${{ parameters.projects }}
      arguments: '--no-restore -c ${{ parameters.configuration }}'
```

Reference templates from the main pipeline:

```yaml
stages:
  - stage: Build
    jobs:
      - job: BuildJob
        steps:
          - template: templates/dotnet-build.yml
            parameters:
              configuration: 'Release'
```

## Variable Groups and Secret Management

Store secrets and environment config in variable groups linked to Azure Key Vault:

```yaml
variables:
  - group: 'escrow-app-common'        # Shared across environments
  - group: 'escrow-app-staging'        # Environment-specific
  - name: localVar
    value: 'inline-value'
```

**Best practices:**
- Link variable groups to Azure Key Vault for automatic secret rotation
- Use separate variable groups per environment (dev, staging, production)
- Mark secrets as `isSecret: true` — they are masked in logs automatically
- Reference secrets with `$(variableName)` syntax — never echo them in scripts
- Use `template` variables (`${{ variables.name }}`) for compile-time substitution

## Service Connections

Configure service connections for deployment authentication:

- **Azure Resource Manager** — federated (OIDC) or service principal for Azure deployments
- **Docker Registry** — ACR, Docker Hub, or private registry for image push/pull
- **Kubernetes** — kubeconfig or Azure Kubernetes Service connection
- **NuGet** — authenticated feed for private package restore/publish

```yaml
- task: AzureWebApp@1
  displayName: 'Deploy to App Service'
  inputs:
    azureSubscription: 'escrow-azure-connection'  # Service connection name
    appType: 'webAppLinux'
    appName: '$(appServiceName)'
    package: '$(Pipeline.Workspace)/drop/**/*.zip'
```

Prefer **Workload Identity Federation (OIDC)** over service principal secrets — no credentials to rotate.

## Environment Approvals and Gates

Define environments with approval workflows and deployment gates:

```yaml
stages:
  - stage: DeployStaging
    jobs:
      - deployment: DeployWeb
        environment: 'staging'
        strategy:
          runOnce:
            deploy:
              steps:
                - script: echo "Deploying to staging"

  - stage: DeployProduction
    dependsOn: DeployStaging
    condition: succeeded()
    jobs:
      - deployment: DeployWeb
        environment: 'production'   # Requires manual approval
        strategy:
          runOnce:
            deploy:
              steps:
                - script: echo "Deploying to production"
```

Configure in Azure DevOps UI:
- **Approvals** — Require one or more reviewers before deployment proceeds
- **Branch control** — Restrict which branches can deploy to an environment
- **Business hours** — Only allow deployments during specified windows
- **Invoke REST API** — Gate on external health check or change management system

## Cache@2 Task

Cache NuGet packages and other dependencies:

```yaml
- task: Cache@2
  displayName: 'Cache NuGet packages'
  inputs:
    key: 'nuget | "$(Agent.OS)" | **/packages.lock.json'
    restoreKeys: |
      nuget | "$(Agent.OS)"
    path: $(NUGET_PACKAGES)

- task: Cache@2
  displayName: 'Cache npm packages'
  inputs:
    key: 'npm | "$(Agent.OS)" | **/package-lock.json'
    restoreKeys: |
      npm | "$(Agent.OS)"
    path: $(npm_config_cache)
```

**Cache key format:** `type | OS | lock-file-hash`. The pipe `|` separator segments are matched left to right for restore keys.

## Multi-Stage Pipeline Example

```yaml
trigger:
  branches:
    include: [main, develop]

pr:
  branches:
    include: [main]

variables:
  - group: 'escrow-app-common'
  - name: buildConfiguration
    value: 'Release'
  - name: dotnetVersion
    value: '10.0.x'

stages:
  # ── Build & Test ──
  - stage: Build
    displayName: 'Build & Test'
    jobs:
      - job: BuildAndTest
        pool:
          vmImage: 'ubuntu-latest'
        timeoutInMinutes: 15
        steps:
          - task: UseDotNet@2
            displayName: 'Install .NET SDK'
            inputs:
              version: $(dotnetVersion)

          - task: Cache@2
            displayName: 'Cache NuGet'
            inputs:
              key: 'nuget | "$(Agent.OS)" | **/packages.lock.json'
              restoreKeys: nuget | "$(Agent.OS)"
              path: $(NUGET_PACKAGES)

          - task: DotNetCoreCLI@2
            displayName: 'Restore'
            inputs:
              command: restore

          - task: DotNetCoreCLI@2
            displayName: 'Build'
            inputs:
              command: build
              arguments: '--no-restore -c $(buildConfiguration) /p:ContinuousIntegrationBuild=true'

          - task: DotNetCoreCLI@2
            displayName: 'Test'
            inputs:
              command: test
              arguments: >
                --no-build -c $(buildConfiguration)
                --logger "trx;LogFileName=results.trx"
                --collect:"XPlat Code Coverage"

          - task: PublishTestResults@2
            displayName: 'Publish Test Results'
            condition: always()
            inputs:
              testResultsFormat: 'VSTest'
              testResultsFiles: '**/*.trx'

          - task: PublishCodeCoverageResults@2
            displayName: 'Publish Coverage'
            inputs:
              summaryFileLocation: '**/coverage.cobertura.xml'

  # ── Deploy Staging ──
  - stage: DeployStaging
    displayName: 'Deploy to Staging'
    dependsOn: Build
    condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
    jobs:
      - deployment: DeployWeb
        environment: 'staging'
        pool:
          vmImage: 'ubuntu-latest'
        timeoutInMinutes: 10
        strategy:
          runOnce:
            deploy:
              steps:
                - task: UseDotNet@2
                  inputs:
                    version: $(dotnetVersion)
                - script: dotnet publish -c $(buildConfiguration) -o $(Build.ArtifactStagingDirectory)
                - task: AzureWebApp@1
                  inputs:
                    azureSubscription: 'escrow-azure-connection'
                    appType: 'webAppLinux'
                    appName: '$(stagingAppName)'
                    package: '$(Build.ArtifactStagingDirectory)'

  # ── Deploy Production ──
  - stage: DeployProduction
    displayName: 'Deploy to Production'
    dependsOn: DeployStaging
    condition: succeeded()
    jobs:
      - deployment: DeployWeb
        environment: 'production'
        pool:
          vmImage: 'ubuntu-latest'
        timeoutInMinutes: 10
        strategy:
          runOnce:
            deploy:
              steps:
                - task: UseDotNet@2
                  inputs:
                    version: $(dotnetVersion)
                - script: dotnet publish -c $(buildConfiguration) -o $(Build.ArtifactStagingDirectory)
                - task: AzureWebApp@1
                  inputs:
                    azureSubscription: 'escrow-azure-connection'
                    appType: 'webAppLinux'
                    appName: '$(productionAppName)'
                    package: '$(Build.ArtifactStagingDirectory)'
```
