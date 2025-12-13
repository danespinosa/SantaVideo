# Santa Video Generator - Usage Guide

## Step-by-Step Instructions

### Step 1: Provision Azure Resources

Open PowerShell and run:

```powershell
cd C:\Users\luespino\source\repos\SantaVideo
.\provision-sora-model.ps1
```

**What this does:**
- Logs you into Azure (if not already logged in)
- Creates a Resource Group for the project
- Creates an Azure OpenAI resource
- Deploys the Sora video generation model
- Saves the configuration to `appsettings.json`

**Expected output:**
```
================================
Azure AI Foundry Sora Model Setup
================================

✓ Azure CLI version: 2.x.x
✓ Logged in as: your@email.com
✓ Subscription: Your Subscription Name

Creating Resource Group: rg-santa-video...
✓ Resource Group created

Creating Azure OpenAI resource: aoai-santa-video-xxxx...
✓ Azure OpenAI resource created

Deploying Sora video generation model: sora-deployment...
✓ Sora model deployment created

================================
Setup Complete!
================================

Configuration saved to: appsettings.json
```

### Step 2: Prepare Your Image

Place a Christmas scene image in the project folder. The image should include:
- A Christmas tree (where Santa will place gifts)
- Indoor or outdoor holiday setting
- Good lighting
- Clear space for Santa to appear

**Supported formats:** JPG, PNG, WebP

Example:
```
C:\Users\luespino\source\repos\SantaVideo\my-christmas-tree.jpg
```

### Step 3: Run the Application

**Option A - Interactive Mode:**
```powershell
dotnet run
```
Then enter the image path when prompted.

**Option B - Command Line:**
```powershell
dotnet run "my-christmas-tree.jpg"
```

Or with full path:
```powershell
dotnet run "C:\Pictures\christmas-scene.png"
```

### Step 4: Wait for Video Generation

The application will:

1. **Upload** your image to Azure
2. **Process** with Sora model (2-5 minutes typically)
3. **Poll** for completion status
4. **Download** the final video

**Console output example:**
```
🎅 Santa Video Generator - Powered by Azure AI Foundry Sora
============================================================

📸 Loading image: my-christmas-tree.jpg

🎬 Generating video with Sora model...
   Prompt: Santa Claus magically appears in the scene, walks gracefully
           to the Christmas tree, places beautifully wrapped gifts underneath,
           steps back to admire the scene, then disappears in a festive sparkle.

⏳ Sending request to Azure AI Foundry Sora...
✓ Video generation started! Operation ID: abc123...

⏳ Generating video (this may take a few minutes)...
   Progress: running (15s elapsed)
   Progress: running (30s elapsed)
   ...

✓ Video generation completed!

📥 Downloading video from: https://...

✅ SUCCESS! Video saved to: santa_video_20251213_142530.mp4
   File size: 8.45 MB

🎄 Your magical Santa video is ready!
```

### Step 5: View Your Video

The generated video will be saved as:
```
santa_video_YYYYMMDD_HHMMSS.mp4
```

Open it with any video player (Windows Media Player, VLC, etc.)

## Customization Options

### Modify the Santa Prompt

Edit `Program.cs` line ~87 to customize Santa's behavior:

```csharp
prompt = "Your custom prompt here describing what Santa should do..."
```

### Change Video Parameters

Edit `Program.cs` lines ~92-96:

```csharp
duration = 15,           // Video length in seconds (5-15)
aspectRatio = "1:1",     // Options: "16:9", "9:16", "1:1"
quality = "standard",    // Options: "standard", "high"
includeAudio = false     // Set to false to disable audio
```

## Troubleshooting

### "Configuration missing"
- Ensure `provision-sora-model.ps1` ran successfully
- Check that `appsettings.json` exists with valid values

### "Deployment failed" during provisioning
- Your subscription may not have Sora access yet (it's in preview)
- Try a different Azure region
- Contact Azure support for preview access

### "Image file not found"
- Use full path to the image
- Check spelling and file extension
- Ensure the file exists

### Video generation takes too long
- Sora processing typically takes 2-5 minutes
- Complex scenes may take longer
- The app polls for 5 minutes before timeout

### "Insufficient quota"
- Check your Azure OpenAI quota in the portal
- Request quota increase if needed

## Cost Estimation

Sora video generation costs vary by:
- Video duration
- Quality setting
- Region

**Approximate costs** (as of Dec 2024):
- ~$0.50 - $2.00 per 10-second video
- Check Azure OpenAI pricing for exact rates

## Tips for Best Results

1. **Use high-quality images** (1920x1080 or higher)
2. **Clear tree placement** - Tree should be prominent
3. **Good lighting** - Well-lit scenes work best
4. **Avoid busy backgrounds** - Keep it simple
5. **Consider composition** - Leave space for Santa to appear

## Example Commands

```powershell
# Generate from local file
dotnet run "christmas-tree.jpg"

# Generate from full path
dotnet run "C:\Users\luespino\Pictures\holiday\tree.png"

# Interactive mode
dotnet run
```

## Next Steps

- Try different Christmas scenes
- Experiment with different prompts
- Adjust video duration and quality
- Share your magical videos! 🎅🎄

---

**Need Help?**
- Check the README.md for more details
- Review Azure OpenAI documentation
- Contact Azure support for access issues
