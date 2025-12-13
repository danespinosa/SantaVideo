# Azure AI Foundry Sora Model Provisioning Script
# This script creates an Azure OpenAI resource and deploys the Sora video generation model

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

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Azure AI Foundry Sora Model Setup" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
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
    # Deploy Sora model (use sora-1 or sora-2 based on availability)
    try {
        az cognitiveservices account deployment create `
            --name $OpenAIName `
            --resource-group $ResourceGroupName `
            --deployment-name $DeploymentName `
            --model-name "sora-2" `
            --model-version "1" `
            --model-format OpenAI `
            --sku-capacity 1 `
            --sku-name "Standard" `
            --output none
        Write-Host "✓ Sora model deployment created" -ForegroundColor Green
    } catch {
        Write-Host "Failed to deploy Sora-2. Trying Sora-1..." -ForegroundColor Yellow
        az cognitiveservices account deployment create `
            --name $OpenAIName `
            --resource-group $ResourceGroupName `
            --deployment-name $DeploymentName `
            --model-name "sora-1" `
            --model-version "1" `
            --model-format OpenAI `
            --sku-capacity 1 `
            --sku-name "Standard" `
            --output none
        Write-Host "✓ Sora-1 model deployment created" -ForegroundColor Green
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
