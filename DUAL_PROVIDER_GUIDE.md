# Dual Provider Support: Azure AI Foundry vs OpenAI Sora 2

The Santa Video Generator now supports **both** Azure AI Foundry Sora AND OpenAI's Sora 2 API.

## Configuration

Edit `appsettings.json` to configure both providers:

```json
{
  "AzureAI": {
    "Endpoint": "https://eastus2.api.cognitive.microsoft.com",
    "ApiKey": "your-azure-api-key",
    "DeploymentName": "sora-deployment"
  },
  "OpenAI": {
    "Endpoint": "https://api.openai.com",
    "ApiKey": "sk-your-openai-api-key",
    "Model": "sora-2"
  },
  "Provider": "Azure"
}
```

### Switch Provider

Change the `"Provider"` field to switch between providers:
- `"Azure"` - Uses Azure AI Foundry Sora (default)
- `"OpenAI"` - Uses OpenAI Sora 2 API

## Provider Differences

### Azure AI Foundry Sora (Sora-1)

**API Format:**
- Uses `multipart/form-data`
- Image uploaded as binary file
- Supports inpainting (anchor first/last frames)
- Job-based async processing

**Endpoints:**
```
POST /openai/v1/video/generations/jobs
GET  /openai/v1/video/generations/jobs/{jobId}
GET  /openai/v1/video/generations/{generationId}/content/video
```

**Parameters:**
- `height`: 480, 720, 1080
- `width`: 854, 1280, 1920
- `n_seconds`: 5, 10, 15
- `model`: "sora"
- `inpaint_items`: JSON array for frame anchoring

**Advantages:**
- ✅ Image-to-video with inpainting
- ✅ Control over background consistency
- ✅ Enterprise Azure integration
- ✅ Runs in your Azure subscription

**Use Case:**
Best for enterprise scenarios where you need to anchor specific frames and maintain background consistency.

---

### OpenAI Sora 2

**API Format:**
- Uses `multipart/form-data` (same as Azure)
- Image uploaded as binary file
- Supports image-to-video
- Video ID-based polling

**Endpoints:**
```
POST /v1/videos
GET  /v1/videos/{videoId}
GET  /v1/videos/{videoId}/download
```

**Parameters:**
- `model`: "sora-2" or "sora-2-pro"
- `prompt`: Text description
- `resolution`: "720p", "1024p" (sora-2-pro only)
- `duration`: 4, 8, or 12 seconds
- `file`: Image file (multipart upload)

**Advantages:**
- ✅ Image-to-video support
- ✅ Access to sora-2-pro (higher quality)
- ✅ Direct OpenAI integration
- ✅ May have newer features faster

**Use Case:**
Best for straightforward image-to-video or text-to-video generation with latest OpenAI features.

---

## Pricing Comparison

### Azure AI Foundry
- Pricing based on your Azure subscription
- Pay-as-you-go through Azure billing
- Check: https://azure.microsoft.com/pricing/details/cognitive-services/openai-service/

### OpenAI Sora 2
- **sora-2**: ~$0.10/second (720p)
- **sora-2-pro**: ~$0.30-0.50/second (720p/1024p)
- 10-second video ≈ $1.00-$5.00
- Billed through OpenAI account

---

## Usage Examples

### Using Azure (Default)

```json
{
  "Provider": "Azure",
  "AzureAI": {
    "Endpoint": "https://eastus2.api.cognitive.microsoft.com",
    "ApiKey": "your-azure-key",
    "DeploymentName": "sora-deployment"
  }
}
```

```bash
dotnet run christmas-tree.jpg
```

### Using OpenAI

```json
{
  "Provider": "OpenAI",
  "OpenAI": {
    "Endpoint": "https://api.openai.com",
    "ApiKey": "sk-your-openai-key",
    "Model": "sora-2"
  }
}
```

```bash
dotnet run christmas-tree.jpg
```

### Using OpenAI Sora 2 Pro

```json
{
  "Provider": "OpenAI",
  "OpenAI": {
    "Endpoint": "https://api.openai.com",
    "ApiKey": "sk-your-openai-key",
    "Model": "sora-2-pro"
  }
}
```

---

## Feature Comparison

| Feature | Azure Sora-1 | OpenAI Sora-2 | OpenAI Sora-2-Pro |
|---------|--------------|---------------|-------------------|
| **Image-to-Video** | ✅ Yes (with inpainting) | ✅ Yes | ✅ Yes |
| **Max Resolution** | 1920x1080 | 1280x720 | 1792x1024 |
| **Duration Options** | 5, 10, 15 sec | 4, 8, 12 sec | 4, 8, 12 sec |
| **Inpainting** | ✅ Yes | ⚠ Limited | ⚠ Limited |
| **API Format** | Multipart | Multipart | Multipart |
| **Price (10s video)** | Variable | ~$1.00 | ~$3.00-$5.00 |
| **Quality** | High | High | Very High |
| **Setup** | Azure subscription | OpenAI API key | OpenAI API key |

---

## Getting OpenAI API Access

1. Sign up at https://platform.openai.com/
2. Navigate to API keys section
3. Create new API key (starts with `sk-...`)
4. Add payment method (Sora 2 is paid-only)
5. Request Sora access if not available (may require invite)

---

## Implementation Details

### Code Structure

```csharp
// Main dispatcher
public async Task GenerateSantaVideo(string imagePath)
{
    if (_provider == "OpenAI")
        await GenerateSantaVideo_OpenAI(imagePath);
    else
        await GenerateSantaVideo_Azure(imagePath);
}

// OpenAI implementation
private async Task GenerateSantaVideo_OpenAI(string imagePath)
{
    // Multipart/form-data API (same as Azure)
    // POST /v1/videos
    // Poll /v1/videos/{id}
    // Download /v1/videos/{id}/download
}

// Azure implementation  
private async Task GenerateSantaVideo_Azure(string imagePath)
{
    // Multipart/form-data API
    // POST /openai/v1/video/generations/jobs
    // Poll /jobs/{jobId}
    // Download /generations/{genId}/content/video
}
```

---

## Troubleshooting

### Azure Issues
- Ensure Azure subscription has Sora access
- Run `provision-sora-model.ps1` first
- Check deployment name matches

### OpenAI Issues
- Ensure API key is valid (starts with `sk-`)
- Check you have Sora 2 access enabled
- Verify payment method is added
- Check model name: "sora-2" or "sora-2-pro"

### Switching Providers
1. Change `"Provider"` in appsettings.json
2. Ensure the selected provider's section is filled out
3. Restart the application

---

## Recommendation

**For this Santa project (image-to-video):**
- ✅ Use **Azure** OR **OpenAI** - Both support image-to-video!
- Azure has advanced inpainting controls (frame anchoring)
- OpenAI may have simpler setup

**For text-to-video projects:**
- ✅ Either provider works
- OpenAI Sora 2 Pro offers highest quality

---

## Future Enhancements

When OpenAI adds image-to-video support to Sora 2, the code is ready to support it with minimal changes!
