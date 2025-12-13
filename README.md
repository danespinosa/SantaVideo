# 🎅 Santa Video Generator

Generate magical videos of Santa Claus appearing in your Christmas scenes using Azure AI Foundry's Sora video generation model.

## Features

- **Image-to-Video Generation**: Transform static Christmas images into animated videos
- **Santa Animation**: Santa magically appears, places gifts under the tree, and disappears
- **Azure AI Foundry Sora**: Powered by OpenAI's advanced video generation model
- **Automated Deployment**: PowerShell script for easy Azure resource provisioning

## Prerequisites

- .NET 9.0 SDK
- Azure subscription with access to Azure OpenAI
- Azure CLI installed
- Access to Sora model (preview access required)

## Quick Start

### 1. Provision Azure Resources

Run the PowerShell script to create Azure OpenAI resource and deploy Sora model:

```powershell
.\provision-sora-model.ps1
```

Optional parameters:
```powershell
.\provision-sora-model.ps1 `
    -ResourceGroupName "my-rg" `
    -Location "eastus2" `
    -OpenAIName "my-openai" `
    -DeploymentName "my-sora-deployment"
```

This script will:
- ✅ Create an Azure Resource Group
- ✅ Deploy Azure OpenAI resource
- ✅ Deploy Sora video generation model
- ✅ Save configuration to `appsettings.json`

### 2. Run the Application

```bash
dotnet run
```

Or provide the image path directly:

```bash
dotnet run "path\to\your\christmas-scene.jpg"
```

### 3. Wait for Magic

The application will:
1. Upload your Christmas scene image
2. Send it to Sora with the Santa animation prompt
3. Poll for video generation completion
4. Download the final video

Output: `santa_video_YYYYMMDD_HHmmss.mp4`

## How It Works

1. **Input**: You provide a static image of a Christmas scene with a tree
2. **Prompt**: The app sends a detailed prompt to Sora describing Santa's actions
3. **Processing**: Sora generates a 10-second video with Santa animation
4. **Output**: High-quality MP4 video with optional audio

## Sora Prompt Used

```
Santa Claus magically appears in the Christmas scene, walks gracefully 
to the Christmas tree with a bag of presents, carefully places beautifully 
wrapped gifts underneath the tree, steps back to admire his work with a 
warm smile, waves goodbye, and disappears in a shower of festive sparkles 
and twinkling lights. The scene is warm, magical, and filled with holiday spirit.
```

## Configuration

Edit `appsettings.json` manually if needed:

```json
{
  "AzureAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key",
    "DeploymentName": "sora-deployment"
  }
}
```

## Video Parameters

- **Duration**: 10 seconds
- **Aspect Ratio**: 16:9
- **Quality**: High
- **Audio**: Enabled (if supported)

## Troubleshooting

### "Deployment failed"
- Ensure your subscription has access to Sora preview
- Check if Sora is available in your selected region
- Try different Azure regions (eastus2, westus, etc.)

### "Image not found"
- Verify the image path is correct
- Supported formats: JPG, PNG, WebP

### "API request failed"
- Check your Azure OpenAI quota
- Verify API key and endpoint in appsettings.json
- Ensure the deployment name matches

## Cost Considerations

Sora video generation is a premium feature. Check Azure OpenAI pricing:
- https://azure.microsoft.com/pricing/details/cognitive-services/openai-service/

## Learn More

- [Azure AI Foundry Sora Documentation](https://learn.microsoft.com/azure/ai-foundry/openai/concepts/video-generation)
- [Sora Video Generation Quickstart](https://learn.microsoft.com/azure/ai-foundry/openai/video-generation-quickstart)
- [Azure OpenAI Service](https://azure.microsoft.com/products/ai-services/openai-service)

## License

MIT License - Feel free to use for your holiday projects! 🎄
