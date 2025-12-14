# Azure AI Foundry Sora Model Provisioning Script
# This script creates an Azure OpenAI resource and deploys the Sora video generation model

#Requires -Version 7.0

param(
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "rg-santa-video",
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "eastus2",
    
    [Parameter(Mandatory=$false)]
    [string]$OpenAIName = "aoai-santa-video-$(Get-Random -Maximum 9999)",
    
    [Parameter(Mandatory=$false)]
    [string]$DeploymentName = "sora-deployment"
)

# Check PowerShell version (belt and suspenders approach)
if ($PSVersionTable.PSVersion.Major -lt 7) {
    Write-Host "================================" -ForegroundColor Red
    Write-Host "ERROR: PowerShell 7+ Required" -ForegroundColor Red
    Write-Host "================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Current Version: PowerShell $($PSVersionTable.PSVersion)" -ForegroundColor Yellow
    Write-Host "Required Version: PowerShell 7.0 or higher" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Please install PowerShell 7+ and run this script again." -ForegroundColor White
    Write-Host ""
    Write-Host "Download PowerShell 7+:" -ForegroundColor Cyan
    Write-Host "  Windows: https://aka.ms/install-powershell-windows" -ForegroundColor White
    Write-Host "  Or use:  winget install Microsoft.PowerShell" -ForegroundColor White
    Write-Host ""
    Write-Host "After installing, run this script with:" -ForegroundColor Cyan
    Write-Host "  pwsh .\provision-sora-model.ps1" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Azure AI Foundry Sora Model Setup" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "✓ PowerShell $($PSVersionTable.PSVersion)" -ForegroundColor Green
Write-Host ""

# Check if Azure CLI is installed
try {
    $azVersion = az version --output json 2>$null | ConvertFrom-Json
    Write-Host "✓ Azure CLI version: $($azVersion.'azure-cli')" -ForegroundColor Green
} catch {
    Write-Error "Azure CLI is not installed. Please install from: https://aka.ms/installazurecliwindows"
    exit 1
}

# Check if logged in
Write-Host "Checking Azure login status..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "Not logged in to Azure. Initiating login..." -ForegroundColor Yellow
    az login
    $account = az account show | ConvertFrom-Json
}
Write-Host "✓ Logged in as: $($account.user.name)" -ForegroundColor Green
Write-Host "✓ Subscription: $($account.name) ($($account.id))" -ForegroundColor Green
Write-Host ""

# Create Resource Group
Write-Host "Creating Resource Group: $ResourceGroupName..." -ForegroundColor Yellow
$rgExists = az group exists --name $ResourceGroupName
if ($rgExists -eq "true") {
    Write-Host "✓ Resource Group already exists" -ForegroundColor Green
} else {
    az group create --name $ResourceGroupName --location $Location --output none
    Write-Host "✓ Resource Group created" -ForegroundColor Green
}
Write-Host ""

# Create Azure OpenAI resource
Write-Host "Creating Azure OpenAI resource: $OpenAIName..." -ForegroundColor Yellow
$openAIExists = az cognitiveservices account show `
    --name $OpenAIName `
    --resource-group $ResourceGroupName 2>$null

if (-not $openAIExists) {
    az cognitiveservices account create `
        --name $OpenAIName `
        --resource-group $ResourceGroupName `
        --location $Location `
        --kind OpenAI `
        --sku S0 `
        --yes `
        --output none
    Write-Host "✓ Azure OpenAI resource created" -ForegroundColor Green
} else {
    Write-Host "✓ Azure OpenAI resource already exists" -ForegroundColor Green
}
Write-Host ""

# Deploy Sora model
Write-Host "Deploying Sora video generation model: $DeploymentName..." -ForegroundColor Yellow
Write-Host "Note: Sora model requires preview access. Ensure your subscription is approved." -ForegroundColor Cyan

$deploymentExists = az cognitiveservices account deployment show `
    --name $OpenAIName `
    --resource-group $ResourceGroupName `
    --deployment-name $DeploymentName 2>$null

if (-not $deploymentExists) {
    # Try Sora-2 first
    Write-Host "Attempting to deploy Sora-2 model..." -ForegroundColor Yellow
    az cognitiveservices account deployment create `
        --name $OpenAIName `
        --resource-group $ResourceGroupName `
        --deployment-name $DeploymentName `
        --model-name "sora-2" `
        --model-version "1" `
        --model-format OpenAI `
        --sku-capacity 1 `
        --sku-name "Standard" `
        --output none 2>$null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Sora-2 model deployment created successfully" -ForegroundColor Green
    } else {
        # Sora-2 failed, try Sora-1
        Write-Host "⚠ Sora-2 not available. Trying Sora-1..." -ForegroundColor Yellow
        az cognitiveservices account deployment create `
            --name $OpenAIName `
            --resource-group $ResourceGroupName `
            --deployment-name $DeploymentName `
            --model-name "sora-1" `
            --model-version "1" `
            --model-format OpenAI `
            --sku-capacity 1 `
            --sku-name "Standard" `
            --output none 2>$null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Sora-1 model deployment created successfully" -ForegroundColor Green
        } else {
            # Both failed, try just "sora"
            Write-Host "⚠ Sora-1 not available. Trying generic 'sora' model..." -ForegroundColor Yellow
            az cognitiveservices account deployment create `
                --name $OpenAIName `
                --resource-group $ResourceGroupName `
                --deployment-name $DeploymentName `
                --model-name "sora" `
                --model-version "1" `
                --model-format OpenAI `
                --sku-capacity 1 `
                --sku-name "Standard" `
                --output none
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓ Sora model deployment created successfully" -ForegroundColor Green
            } else {
                Write-Host "❌ Failed to deploy Sora model. Please check:" -ForegroundColor Red
                Write-Host "   1. Your subscription has access to Sora preview" -ForegroundColor Yellow
                Write-Host "   2. The region '$Location' supports Sora models" -ForegroundColor Yellow
                Write-Host "   3. Your Azure OpenAI quota is sufficient" -ForegroundColor Yellow
                Write-Host ""
                Write-Host "Try running with a different region:" -ForegroundColor Cyan
                Write-Host "   .\provision-sora-model.ps1 -Location 'eastus2'" -ForegroundColor White
                Write-Host ""
                exit 1
            }
        }
    }
} else {
    Write-Host "✓ Model deployment already exists" -ForegroundColor Green
}
Write-Host ""

# Get endpoint and key
Write-Host "Retrieving connection details..." -ForegroundColor Yellow
$endpoint = az cognitiveservices account show `
    --name $OpenAIName `
    --resource-group $ResourceGroupName `
    --query properties.endpoint `
    --output tsv

$key = az cognitiveservices account keys list `
    --name $OpenAIName `
    --resource-group $ResourceGroupName `
    --query key1 `
    --output tsv

Write-Host ""
Write-Host "================================" -ForegroundColor Green
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""
Write-Host "Configuration Details:" -ForegroundColor Cyan
Write-Host "  Resource Group: $ResourceGroupName" -ForegroundColor White
Write-Host "  OpenAI Resource: $OpenAIName" -ForegroundColor White
Write-Host "  Deployment: $DeploymentName" -ForegroundColor White
Write-Host "  Endpoint: $endpoint" -ForegroundColor White
Write-Host "  API Key: $($key.Substring(0,8))..." -ForegroundColor White
Write-Host ""

# Save configuration to appsettings.json
$appSettings = @{
    AzureAI = @{
        Endpoint = $endpoint
        ApiKey = $key
        DeploymentName = $DeploymentName
    }
} | ConvertTo-Json -Depth 3

$configPath = Join-Path $PSScriptRoot "appsettings.json"
$appSettings | Out-File -FilePath $configPath -Encoding UTF8
Write-Host "✓ Configuration saved to: $configPath" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Run 'dotnet run --project SantaVideo.csproj' to use the .NET app" -ForegroundColor White
Write-Host "2. Place your image in the project directory" -ForegroundColor White
Write-Host "3. The app will generate a video with Santa placing gifts" -ForegroundColor White
