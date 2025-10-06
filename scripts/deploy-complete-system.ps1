# =============================================================================
# Oil Trading System - Complete Production Deployment Script (PowerShell)
# =============================================================================
# This script deploys the entire Oil Trading System to Kubernetes
# Supports multiple environments: development, staging, production
# =============================================================================

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("development", "staging", "production")]
    [string]$Environment = "staging",
    
    [Parameter(Mandatory=$false)]
    [string]$Namespace = "",
    
    [Parameter(Mandatory=$false)]
    [string]$ReleaseName = "",
    
    [Parameter(Mandatory=$false)]
    [string]$ImageTag = "latest",
    
    [Parameter(Mandatory=$false)]
    [string]$ValuesFile = "",
    
    [Parameter(Mandatory=$false)]
    [int]$Timeout = 600,
    
    [Parameter(Mandatory=$false)]
    [switch]$DryRun,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipTests,
    
    [Parameter(Mandatory=$false)]
    [switch]$NoBackup,
    
    [Parameter(Mandatory=$false)]
    [switch]$Force,
    
    [Parameter(Mandatory=$false)]
    [switch]$Debug,
    
    [Parameter(Mandatory=$false)]
    [switch]$Help
)

# Script metadata
$ScriptVersion = "1.0.0"
$ScriptName = "Oil Trading System Deployment"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

# Set default values
if ([string]::IsNullOrEmpty($Namespace)) {
    $Namespace = "oil-trading-$Environment"
}

if ([string]::IsNullOrEmpty($ReleaseName)) {
    $ReleaseName = "oil-trading-$Environment"
}

if ([string]::IsNullOrEmpty($ValuesFile)) {
    $ValuesFile = "$ProjectRoot\helm\oil-trading-system\values-$Environment.yaml"
}

$ChartPath = "$ProjectRoot\helm\oil-trading-system"

# Logging functions
function Write-LogInfo {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

function Write-LogSuccess {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-LogWarning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-LogError {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Write-LogDebug {
    param([string]$Message)
    if ($Debug) {
        Write-Host "[DEBUG] $Message" -ForegroundColor Magenta
    }
}

# Banner function
function Show-Banner {
    Write-Host @"
╔═══════════════════════════════════════════════════════════════════════════════╗
║                        Oil Trading System Deployment                         ║
║                     Enterprise Trading & Risk Management                     ║
╚═══════════════════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

    Write-Host "Version: $ScriptVersion" -ForegroundColor Blue
    Write-Host "Environment: $Environment" -ForegroundColor Blue
    Write-Host "Namespace: $Namespace" -ForegroundColor Blue
    Write-Host ""
}

# Usage function
function Show-Usage {
    Write-Host @"
$ScriptName v$ScriptVersion

DESCRIPTION:
    Deploys the Oil Trading System to Kubernetes cluster

PARAMETERS:
    -Environment        Target environment (development|staging|production) [default: staging]
    -Namespace          Kubernetes namespace [default: oil-trading-ENV]
    -ReleaseName        Helm release name [default: oil-trading-ENV]
    -ImageTag           Docker image tag [default: latest]
    -ValuesFile         Custom values file [default: values-ENV.yaml]
    -Timeout            Deployment timeout in seconds [default: 600]
    -DryRun             Perform a dry run without making changes
    -SkipTests          Skip pre-deployment tests
    -NoBackup           Skip database backup (production only)
    -Force              Force deployment even if validation fails
    -Debug              Enable debug output
    -Help               Show this help message

EXAMPLES:
    # Deploy to staging
    .\deploy-complete-system.ps1 -Environment staging

    # Deploy to production with specific image tag
    .\deploy-complete-system.ps1 -Environment production -ImageTag v1.2.3

    # Dry run for production
    .\deploy-complete-system.ps1 -Environment production -DryRun

    # Force deployment with debug output
    .\deploy-complete-system.ps1 -Environment staging -Force -Debug
"@
}

# Validation functions
function Test-Environment {
    Write-LogInfo "Validating environment configuration..."
    
    if ($Environment -in @("development", "staging", "production")) {
        Write-LogSuccess "Environment '$Environment' is valid"
    } else {
        Write-LogError "Invalid environment: $Environment"
        Write-LogError "Valid environments: development, staging, production"
        exit 1
    }
}

function Test-RequiredTools {
    Write-LogInfo "Validating required tools..."
    
    $RequiredTools = @("kubectl", "helm", "docker")
    $MissingTools = @()
    
    foreach ($Tool in $RequiredTools) {
        if (-not (Get-Command $Tool -ErrorAction SilentlyContinue)) {
            $MissingTools += $Tool
        }
    }
    
    if ($MissingTools.Count -gt 0) {
        Write-LogError "Missing required tools: $($MissingTools -join ', ')"
        Write-LogError "Please install missing tools and try again"
        exit 1
    }
    
    Write-LogSuccess "All required tools are available"
}

function Test-KubernetesConnection {
    Write-LogInfo "Validating Kubernetes configuration..."
    
    try {
        $ClusterInfo = kubectl cluster-info 2>$null
        if ($LASTEXITCODE -ne 0) {
            throw "Cannot connect to cluster"
        }
        
        $Context = kubectl config current-context
        Write-LogSuccess "Connected to Kubernetes cluster (context: $Context)"
        
        # Validate environment-specific context
        if ($Environment -eq "production" -and $Context -notlike "*production*") {
            if (-not $Force) {
                Write-LogError "Production deployment requires production context"
                Write-LogError "Current context: $Context"
                Write-LogError "Use -Force to override this check"
                exit 1
            } else {
                Write-LogWarning "Production deployment forced with non-production context"
            }
        }
    }
    catch {
        Write-LogError "Cannot connect to Kubernetes cluster"
        Write-LogError "Please check your kubeconfig and try again"
        exit 1
    }
}

function Test-DeploymentFiles {
    Write-LogInfo "Validating deployment files..."
    
    $RequiredFiles = @(
        $ValuesFile,
        "$ChartPath\Chart.yaml",
        "$ChartPath\values.yaml"
    )
    
    foreach ($File in $RequiredFiles) {
        if (-not (Test-Path $File)) {
            Write-LogError "Required file not found: $File"
            exit 1
        }
    }
    
    Write-LogSuccess "All required files are available"
}

function Test-HelmChart {
    Write-LogInfo "Validating Helm chart..."
    
    try {
        $LintResult = helm lint $ChartPath -f $ValuesFile 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-LogError "Helm chart validation failed"
            Write-Host $LintResult
            exit 1
        }
        
        Write-LogSuccess "Helm chart validation passed"
    }
    catch {
        Write-LogError "Helm chart validation failed: $_"
        exit 1
    }
}

# Pre-deployment functions
function New-NamespaceIfNotExists {
    Write-LogInfo "Creating namespace if not exists..."
    
    $NamespaceExists = kubectl get namespace $Namespace 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-LogInfo "Namespace '$Namespace' already exists"
    } else {
        if ($DryRun) {
            Write-LogInfo "[DRY RUN] Would create namespace: $Namespace"
        } else {
            kubectl create namespace $Namespace
            kubectl label namespace $Namespace "environment=$Environment" --overwrite
            Write-LogSuccess "Created namespace: $Namespace"
        }
    }
}

function Set-ExternalSecrets {
    Write-LogInfo "Setting up external secrets..."
    
    if ($Environment -eq "production") {
        Write-LogInfo "Applying external secrets configuration..."
        
        if ($DryRun) {
            Write-LogInfo "[DRY RUN] Would apply external secrets"
        } else {
            try {
                kubectl apply -f "$ProjectRoot\k8s\external-secrets\" -n $Namespace
                Write-LogSuccess "External secrets configuration applied"
            }
            catch {
                Write-LogWarning "External secrets application failed: $_"
            }
        }
    } else {
        Write-LogInfo "External secrets skipped for non-production environment"
    }
}

function New-DatabaseBackup {
    if ($Environment -ne "production" -or $NoBackup) {
        Write-LogInfo "Backup skipped for environment: $Environment"
        return
    }
    
    Write-LogInfo "Creating database backup..."
    
    $BackupName = "backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    
    if ($DryRun) {
        Write-LogInfo "[DRY RUN] Would create backup: $BackupName"
    } else {
        try {
            # Create backup job
            kubectl create job $BackupName --from=cronjob/postgresql-backup -n $Namespace
            
            # Wait for backup completion
            kubectl wait --for=condition=complete job/$BackupName -n $Namespace --timeout=300s
            
            Write-LogSuccess "Database backup created: $BackupName"
        }
        catch {
            Write-LogWarning "Database backup failed: $_"
        }
    }
}

# Testing functions
function Invoke-PreDeploymentTests {
    if ($SkipTests) {
        Write-LogInfo "Pre-deployment tests skipped"
        return
    }
    
    Write-LogInfo "Running pre-deployment tests..."
    
    # Template Helm chart to validate
    Write-LogInfo "Validating Helm templates..."
    try {
        $null = helm template $ReleaseName $ChartPath -f $ValuesFile --namespace $Namespace --set "image.tag=$ImageTag" 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Helm template validation failed"
        }
        
        # Validate Kubernetes resources
        Write-LogInfo "Validating Kubernetes resources..."
        helm template $ReleaseName $ChartPath -f $ValuesFile --namespace $Namespace --set "image.tag=$ImageTag" | kubectl apply --dry-run=client -f -
        
        Write-LogSuccess "Pre-deployment tests passed"
    }
    catch {
        Write-LogError "Pre-deployment tests failed: $_"
        exit 1
    }
}

# Deployment functions
function Deploy-MonitoringStack {
    Write-LogInfo "Deploying monitoring stack..."
    
    if ($DryRun) {
        Write-LogInfo "[DRY RUN] Would deploy monitoring stack"
    } else {
        try {
            # Apply Prometheus Operator
            kubectl apply -f "$ProjectRoot\k8s\monitoring\prometheus-operator.yaml"
            
            # Apply Grafana dashboards
            kubectl apply -f "$ProjectRoot\k8s\monitoring\grafana-dashboards.yaml"
            
            Write-LogSuccess "Monitoring stack deployed"
        }
        catch {
            Write-LogWarning "Monitoring stack deployment failed: $_"
        }
    }
}

function Deploy-Application {
    Write-LogInfo "Deploying Oil Trading System..."
    
    $HelmArgs = @(
        $ReleaseName,
        $ChartPath,
        "--namespace", $Namespace,
        "--create-namespace",
        "--values", $ValuesFile,
        "--set", "image.tag=$ImageTag",
        "--set", "global.environment=$Environment",
        "--timeout", "${Timeout}s",
        "--atomic",
        "--wait"
    )
    
    if ($DryRun) {
        $HelmArgs += "--dry-run"
        Write-LogInfo "[DRY RUN] Helm deployment simulation"
    }
    
    if ($Debug) {
        $HelmArgs += "--debug"
    }
    
    try {
        # Check if release exists
        $ReleaseExists = helm list -n $Namespace | Select-String $ReleaseName
        
        if ($ReleaseExists) {
            Write-LogInfo "Upgrading existing release..."
            helm upgrade @HelmArgs
        } else {
            Write-LogInfo "Installing new release..."
            helm install @HelmArgs
        }
        
        if (-not $DryRun) {
            Write-LogSuccess "Application deployed successfully"
        }
    }
    catch {
        Write-LogError "Application deployment failed: $_"
        exit 1
    }
}

# Post-deployment functions
function Invoke-SmokeTests {
    if ($DryRun -or $SkipTests) {
        Write-LogInfo "Smoke tests skipped"
        return
    }
    
    Write-LogInfo "Running smoke tests..."
    
    try {
        # Wait for pods to be ready
        Write-LogInfo "Waiting for pods to be ready..."
        kubectl wait --for=condition=ready pod -l "app.kubernetes.io/name=oil-trading-system" -n $Namespace --timeout=300s
        
        # Test API health endpoint
        Write-LogInfo "Testing API health endpoint..."
        $TestPodName = "smoke-test-$(Get-Date -Format 'HHmmss')"
        kubectl run $TestPodName --image=curlimages/curl:latest --rm -i --restart=Never -n $Namespace -- curl -f "http://$ReleaseName-api:8080/health" --max-time 10 --retry 3
        
        # Test frontend
        Write-LogInfo "Testing frontend..."
        $FrontendTestPodName = "frontend-test-$(Get-Date -Format 'HHmmss')"
        kubectl run $FrontendTestPodName --image=curlimages/curl:latest --rm -i --restart=Never -n $Namespace -- curl -f "http://$ReleaseName-frontend:8080/health" --max-time 10 --retry 3
        
        Write-LogSuccess "Smoke tests passed"
    }
    catch {
        Write-LogError "Smoke tests failed: $_"
        exit 1
    }
}

function Test-Deployment {
    if ($DryRun) {
        Write-LogInfo "[DRY RUN] Deployment verification skipped"
        return
    }
    
    Write-LogInfo "Verifying deployment..."
    
    try {
        # Check Helm release status
        $ReleaseStatus = (helm status $ReleaseName -n $Namespace -o json | ConvertFrom-Json).info.status
        if ($ReleaseStatus -ne "deployed") {
            throw "Helm release status: $ReleaseStatus"
        }
        
        # Check pod status
        $PodStatus = kubectl get pods -n $Namespace -l "app.kubernetes.io/name=oil-trading-system" --no-headers
        $FailedPods = ($PodStatus | Where-Object { $_ -notlike "*Running*" }).Count
        
        if ($FailedPods -gt 0) {
            Write-LogError "$FailedPods pod(s) are not running"
            kubectl get pods -n $Namespace -l "app.kubernetes.io/name=oil-trading-system"
            exit 1
        }
        
        Write-LogSuccess "Deployment verification completed"
    }
    catch {
        Write-LogError "Deployment verification failed: $_"
        exit 1
    }
}

# Notification function
function Send-Notification {
    param(
        [string]$Status,
        [string]$Message
    )
    
    $SlackWebhookUrl = $env:SLACK_WEBHOOK_URL
    if ([string]::IsNullOrEmpty($SlackWebhookUrl)) {
        Write-LogDebug "Slack webhook not configured, skipping notification"
        return
    }
    
    $Color = "good"
    $Emoji = "✅"
    
    switch ($Status) {
        "failure" {
            $Color = "danger"
            $Emoji = "❌"
        }
        "warning" {
            $Color = "warning"
            $Emoji = "⚠️"
        }
    }
    
    $Payload = @{
        attachments = @(
            @{
                color = $Color
                title = "$Emoji Oil Trading System Deployment"
                fields = @(
                    @{ title = "Environment"; value = $Environment; short = $true },
                    @{ title = "Namespace"; value = $Namespace; short = $true },
                    @{ title = "Image Tag"; value = $ImageTag; short = $true },
                    @{ title = "Status"; value = $Status; short = $true }
                )
                text = $Message
                footer = "Oil Trading System Deployment Script"
                ts = [int64](Get-Date -UFormat %s)
            }
        )
    } | ConvertTo-Json -Depth 10
    
    try {
        Invoke-RestMethod -Uri $SlackWebhookUrl -Method Post -Body $Payload -ContentType "application/json" | Out-Null
        Write-LogDebug "Notification sent successfully"
    }
    catch {
        Write-LogWarning "Failed to send notification: $_"
    }
}

# Main function
function Main {
    # Show help if requested
    if ($Help) {
        Show-Usage
        return
    }
    
    # Show banner
    Show-Banner
    
    try {
        # Validation phase
        Write-LogInfo "=== VALIDATION PHASE ==="
        Test-Environment
        Test-RequiredTools
        Test-KubernetesConnection
        Test-DeploymentFiles
        Test-HelmChart
        
        # Pre-deployment phase
        Write-LogInfo "=== PRE-DEPLOYMENT PHASE ==="
        New-NamespaceIfNotExists
        Set-ExternalSecrets
        New-DatabaseBackup
        Invoke-PreDeploymentTests
        
        # Deployment phase
        Write-LogInfo "=== DEPLOYMENT PHASE ==="
        Deploy-MonitoringStack
        Deploy-Application
        
        # Post-deployment phase
        Write-LogInfo "=== POST-DEPLOYMENT PHASE ==="
        Test-Deployment
        Invoke-SmokeTests
        
        # Success
        Write-LogSuccess "=== DEPLOYMENT COMPLETED SUCCESSFULLY ==="
        Send-Notification "success" "Deployment completed successfully in $Environment environment."
        
        # Display information
        Write-Host ""
        Write-LogInfo "Deployment Information:"
        Write-LogInfo "  Environment: $Environment"
        Write-LogInfo "  Namespace: $Namespace"
        Write-LogInfo "  Release: $ReleaseName"
        Write-LogInfo "  Image Tag: $ImageTag"
        
        if ($Environment -eq "production") {
            Write-LogInfo "  Production URL: https://oiltrading.example.com"
            Write-LogInfo "  API URL: https://api.oiltrading.example.com"
        } elseif ($Environment -eq "staging") {
            Write-LogInfo "  Staging URL: https://staging.oiltrading.example.com"
            Write-LogInfo "  API URL: https://staging-api.oiltrading.example.com"
        }
        
        Write-Host ""
        Write-LogSuccess "Oil Trading System deployment completed! 🎉"
    }
    catch {
        Write-LogError "Deployment failed: $_"
        Send-Notification "failure" "Deployment failed. Please check the logs for details."
        exit 1
    }
}

# Execute main function
Main