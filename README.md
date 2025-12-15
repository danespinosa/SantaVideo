# 🎅 Santa Video Generator

Generate magical videos of Santa Claus appearing in your Christmas scenes using Azure AI Foundry's Sora video generation model.

## Features

- **Image-to-Video Generation**: Transform static Christmas images into animated videos
- **Santa Animation**: Santa magically appears, places gifts under the tree, and disappears
- **Azure AI Foundry Sora**: Powered by OpenAI's Sora-1 advanced video generation model
- **Automated Deployment**: PowerShell script for easy Azure resource provisioning
- **Smart Model Discovery**: Automatically finds and deploys available Sora models
- **Job Management**: Download videos from completed jobs with `download-sora-videos.ps1`

## Prerequisites

- .NET 9.0 SDK
- PowerShell 7.0 or higher
- Azure subscription with access to Azure OpenAI
- Azure CLI installed
- Access to Sora model (preview access required)

## Quick Start

### 1. Provision Azure Resources

Run the PowerShell script to create Azure OpenAI resource and deploy Sora model:

```powershell
pwsh .\provision-sora-model.ps1
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
- ✅ Check PowerShell 7+ requirement
- ✅ Create an Azure Resource Group
- ✅ Deploy Azure OpenAI resource
- ✅ Auto-discover and deploy Sora model (tries sora-2, sora-1, or sora)
- ✅ Save configuration to `appsettings.json`

### 2. Run the Application

Interactive mode:
```bash
dotnet run
```

Or provide the image path directly:
```bash
dotnet run "path\to\your\christmas-scene.jpg"
```

### 3. Wait for Magic

The application will:
1. Upload your Christmas scene image (as multipart/form-data)
2. Send it to Sora with the Santa animation prompt
3. Create a video generation job
4. Poll job status every 5 seconds (max 10 minutes)
5. Download the final video when complete

Output: `santa_video_YYYYMMDD_HHmmss.mp4`

## How It Works

1. **Input**: You provide a static image of a Christmas scene with a tree
2. **Prompt**: The app sends a detailed prompt to Sora describing Santa's actions
3. **Processing**: Sora generates a 10-second video with Santa animation using image inpainting
4. **Output**: 480p MP4 video (854x480, 16:9 aspect ratio)

## Sora Prompt Used

```
Santa Claus magically appears in the Christmas scene, walks gracefully 
to the Christmas tree with a bag of presents, carefully places beautifully 
wrapped gifts underneath the tree, steps back to admire his work with a 
warm smile, waves goodbye, and disappears in a shower of festive sparkles 
and twinkling lights. The scene is warm, magical, and filled with holiday spirit.
```

## Video Parameters (Current Settings)

- **Resolution**: 854x480 (480p)
- **Aspect Ratio**: 16:9
- **Duration**: 10 seconds
- **Variants**: 1
- **Model**: sora
- **Format**: MP4
- **Inpaint Method**: Image anchored at first and last frame

### Inpaint Configuration

The video starts and ends with your original image:
- **Frame 0** (start): Your Christmas tree scene
- **Frame -1** (end): Your Christmas tree scene (same as start)
- **Frames 1-N**: Sora generates Santa animation

This creates a seamless loop effect! See `INPAINT_GUIDE.md` for advanced options to keep the background consistent throughout the entire video.

## Additional Tools

### Download Videos from Completed Jobs

Use the download script to retrieve videos from previously completed jobs:

```powershell
# Interactive mode - choose from succeeded jobs
.\download-sora-videos.ps1

# List recent jobs
.\download-sora-videos.ps1 -ListJobs

# Download specific job
.\download-sora-videos.ps1 -JobId "job_abc123"

# Download to specific folder
.\download-sora-videos.ps1 -JobId "job_abc123" -OutputDirectory "C:\Videos"
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

## API Compliance

This application uses **Sora 1 API** with:
- **Multipart/form-data** requests (not JSON)
- **Image file upload** (not base64)
- **Job-based polling** (`/openai/v1/video/generations/jobs`)
- **Generation ID download** (`/openai/v1/video/generations/{id}/content/video`)

See `SORA_API_CHANGES.md` for detailed API documentation.

## Customization

### Change Video Resolution

Edit `Program.cs` around line 94:
```csharp
formData.Add(new StringContent("480"), "height");  // 480p, 720p, or 1080p
formData.Add(new StringContent("854"), "width");   // Adjust for aspect ratio
```

### Change Video Duration

Edit `Program.cs` around line 96:
```csharp
formData.Add(new StringContent("10"), "n_seconds");  // 5-15 seconds
```

### Modify Santa's Actions

Edit the `prompt` variable in `Program.cs` around line 88 to change what Santa does.

### Advanced Inpaint Options

See `INPAINT_GUIDE.md` for options to:
- Keep background consistent throughout video
- Constrain specific areas (e.g., just the tree)
- Add middle frame constraints for smoother animation

## Troubleshooting

### "PowerShell 7+ Required"
- Install PowerShell 7: `winget install Microsoft.PowerShell`
- Run with: `pwsh .\provision-sora-model.ps1`

### "Deployment failed"
- Ensure your subscription has access to Sora preview
- Check if Sora is available in your selected region
- Try different Azure regions (eastus2, westus, etc.)
- The script will try: sora-2 → sora-1 → sora (automatic fallback)

### "Image not found"
- Verify the image path is correct
- Supported formats: JPG, PNG, WebP

### "API request failed"
- Check your Azure OpenAI quota
- Verify API key and endpoint in appsettings.json
- Ensure the deployment name matches

### Job takes too long
- Sora typically takes 2-5 minutes for 10-second videos
- App polls for up to 10 minutes (120 attempts × 5 seconds)
- Complex scenes may take longer

## Cost Considerations

Sora video generation is a premium feature:
- 480p videos are faster and cheaper than 720p/1080p
- Typical cost: ~$0.50-$2.00 per 10-second video (estimate)
- Check Azure OpenAI pricing: https://azure.microsoft.com/pricing/details/cognitive-services/openai-service/

## Documentation

- `README.md` - This file (overview and quick start)
- `USAGE.md` - Detailed step-by-step usage guide
- `QUICKSTART.txt` - Quick reference card
- `SORA_API_CHANGES.md` - API implementation details
- `INPAINT_GUIDE.md` - Advanced inpaint configuration options
- `GITHUB_UPLOAD.md` - How to upload project to GitHub

## Learn More

- [Azure AI Foundry Sora Documentation](https://learn.microsoft.com/azure/ai-foundry/openai/concepts/video-generation)
- [Sora Video Generation Quickstart](https://learn.microsoft.com/azure/ai-foundry/openai/video-generation-quickstart)
- [Azure OpenAI Service](https://azure.microsoft.com/products/ai-services/openai-service)

## Project Structure

```
SantaVideo/
├── Program.cs                    # Main application (Sora API calls)
├── SantaVideo.csproj            # .NET project file
├── appsettings.json             # Azure configuration (auto-generated)
├── provision-sora-model.ps1     # Azure provisioning script
├── download-sora-videos.ps1     # Download completed jobs script
├── README.md                    # Project documentation
├── USAGE.md                     # Detailed usage guide
├── QUICKSTART.txt               # Quick reference
├── SORA_API_CHANGES.md          # API implementation details
├── INPAINT_GUIDE.md             # Inpaint configuration guide
└── GITHUB_UPLOAD.md             # GitHub upload instructions
```

## License

MIT License - Feel free to use for your holiday projects! 🎄
