using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var provider = config["Provider"] ?? "Azure";
        
        Console.WriteLine($"🎅 Santa Video Generator - Powered by {provider} Sora");
        Console.WriteLine("============================================================\n");

        string? endpoint, apiKey, modelOrDeployment;
        
        if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = config["OpenAI:Endpoint"];
            apiKey = config["OpenAI:ApiKey"];
            modelOrDeployment = config["OpenAI:Model"];
            
            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(modelOrDeployment))
            {
                Console.WriteLine("❌ OpenAI configuration missing! Please configure OpenAI section in appsettings.json");
                return;
            }
        }
        else // Azure
        {
            endpoint = config["AzureAI:Endpoint"];
            apiKey = config["AzureAI:ApiKey"];
            modelOrDeployment = config["AzureAI:DeploymentName"];
            
            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(modelOrDeployment))
            {
                Console.WriteLine("❌ Azure configuration missing! Please run provision-sora-model.ps1 first.");
                return;
            }
        }

        string? imagePath = null;
        if (args.Length > 0)
        {
            imagePath = args[0];
        }
        else
        {
            Console.Write("Enter the path to your Christmas scene image: ");
            imagePath = Console.ReadLine();
        }

        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
        {
            Console.WriteLine("❌ Image file not found!");
            return;
        }

        var videoGenerator = new SantaVideoGenerator(endpoint, apiKey, modelOrDeployment, provider);
        await videoGenerator.GenerateSantaVideo(imagePath);
    }
}

public class SantaVideoGenerator
{
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _modelOrDeployment;
    private readonly string _provider;
    private readonly HttpClient _httpClient;

    public SantaVideoGenerator(string endpoint, string apiKey, string modelOrDeployment, string provider)
    {
        _endpoint = endpoint;
        _apiKey = apiKey;
        _modelOrDeployment = modelOrDeployment;
        _provider = provider;
        _httpClient = new HttpClient();
        
        // Set authentication header based on provider
        if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }
        else // Azure
        {
            _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
        }
    }

    public async Task GenerateSantaVideo(string imagePath)
    {
        if (_provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            await GenerateSantaVideo_OpenAI(imagePath);
        }
        else // Azure
        {
            await GenerateSantaVideo_Azure(imagePath);
        }
    }

    private async Task GenerateSantaVideo_OpenAI(string imagePath)
    {
        try
        {
            Console.WriteLine($"📸 Loading image: {Path.GetFileName(imagePath)}");
            
            var imageBytes = await File.ReadAllBytesAsync(imagePath);
            var fileName = Path.GetFileName(imagePath);
            var imageExtension = Path.GetExtension(imagePath).TrimStart('.').ToLower();
            var mimeType = imageExtension switch
            {
                "jpg" or "jpeg" => "image/jpeg",
                "png" => "image/png",
                "webp" => "image/webp",
                _ => "image/jpeg"
            };

            Console.WriteLine("\n🎬 Generating video with OpenAI Sora 2...");
            Console.WriteLine("   Prompt: Santa Claus magically appears in the scene...\n");

            var prompt = "Santa Claus magically appears in the Christmas scene, walks gracefully to the Christmas tree with a bag of presents, carefully places beautifully wrapped gifts underneath the tree, steps back to admire his work with a warm smile, waves goodbye, and disappears in a shower of festive sparkles and twinkling lights. The scene is warm, magical, and filled with holiday spirit.";

            // OpenAI Sora 2 ALSO uses multipart/form-data (like Azure)
            using var formData = new MultipartFormDataContent();
            
            // Add form fields
            formData.Add(new StringContent(prompt), "prompt");
            formData.Add(new StringContent(_modelOrDeployment), "model"); // "sora-2" or "sora-2-pro"
            formData.Add(new StringContent("1280x720"), "resolution"); // "720p" or "1024p"
            formData.Add(new StringContent("10"), "seconds"); // 4, 8, or 12
            
            // Add image file if provided
            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
            formData.Add(imageContent, "input_reference", fileName);

            var apiUrl = $"{_endpoint.TrimEnd('/')}/v1/videos";
            
            Console.WriteLine("⏳ Sending request to OpenAI Sora 2...{0}", apiUrl);
            var response = await _httpClient.PostAsync(apiUrl, formData);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Error: {response.StatusCode}");
                Console.WriteLine($"Details: {errorContent}");
                return;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response: {responseJson}");
            
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (result.TryGetProperty("id", out var videoId))
            {
                Console.WriteLine($"✓ Video generation started! Video ID: {videoId.GetString()}");
                await PollVideoStatus_OpenAI(videoId.GetString()!);
            }
            else
            {
                Console.WriteLine($"⚠ Unexpected response format: {responseJson}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private async Task PollVideoStatus_OpenAI(string videoId)
    {
        var statusUrl = $"{_endpoint.TrimEnd('/')}/v1/videos/{videoId}";
        var maxAttempts = 120;
        var attempt = 0;
        string? status = null;

        Console.WriteLine("\n⏳ Generating video (this may take a few minutes)...");

        while (status != "completed" && status != "failed" && status != "cancelled" && attempt < maxAttempts)
        {
            await Task.Delay(5000);
            attempt++;

            var response = await _httpClient.GetAsync(statusUrl);
            var responseJson = await response.Content.ReadAsStringAsync();
            var statusResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (statusResponse.TryGetProperty("status", out var statusValue))
            {
                status = statusValue.GetString();
                Console.Write($"\r   Status: {status} ({attempt * 5}s elapsed)");
                
                if (status == "completed")
                {
                    Console.WriteLine("\n✓ Video generation completed!");
                    await DownloadVideo_OpenAI(videoId);
                    return;
                }
                else if (status == "failed" || status == "cancelled")
                {
                    Console.WriteLine($"\n❌ Video generation {status}.");
                    if (statusResponse.TryGetProperty("error", out var error))
                    {
                        Console.WriteLine($"Error: {error}");
                    }
                    return;
                }
            }
        }

        Console.WriteLine("\n⏰ Timeout waiting for video generation.");
    }

    private async Task DownloadVideo_OpenAI(string videoId)
    {
        try
        {
            var videoUrl = $"{_endpoint.TrimEnd('/')}/v1/videos/{videoId}/content";
            
            Console.WriteLine($"\n📥 Downloading video: {videoId}");
            
            var videoBytes = await _httpClient.GetByteArrayAsync(videoUrl);
            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"santa_video_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
            
            await File.WriteAllBytesAsync(outputPath, videoBytes);
            
            Console.WriteLine($"\n✅ SUCCESS! Video saved to: {outputPath}");
            Console.WriteLine($"   File size: {videoBytes.Length / 1024 / 1024:F2} MB");
            Console.WriteLine($"\n🎄 Your magical Santa video is ready!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error downloading video: {ex.Message}");
        }
    }

    private async Task GenerateSantaVideo_Azure(string imagePath)
    {
        try
        {
            Console.WriteLine($"📸 Loading image: {Path.GetFileName(imagePath)}");
            
            var imageBytes = await File.ReadAllBytesAsync(imagePath);
            var fileName = Path.GetFileName(imagePath);
            var imageExtension = Path.GetExtension(imagePath).TrimStart('.').ToLower();
            var mimeType = imageExtension switch
            {
                "jpg" or "jpeg" => "image/jpeg",
                "png" => "image/png",
                "webp" => "image/webp",
                _ => "image/jpeg"
            };

            Console.WriteLine("\n🎬 Generating video with Sora model...");
            Console.WriteLine("   Prompt: Santa Claus magically appears in the scene, walks gracefully");
            Console.WriteLine("           to the Christmas tree, places beautifully wrapped gifts underneath,");
            Console.WriteLine("           steps back to admire the scene, then disappears in a festive sparkle.\n");

            var prompt = "Use the uploaded image as the fixed background. Do not modify or replace any part of the scene. The image shows a cozy living room with a decorated Christmas tree in the corner, a red tree skirt, a toy car, and a diaper bin. Santa Claus, wearing a classic red suit with white trim and a natural white beard and hair, they should look like hair rather than cotton, and with a big belly, he gently places wrapped presents under the tree. The camera remains steady in a medium shot, and only Santa is animated. His movements are realistic and proportionate, with a cheerful expression. The background stays exactly as shown in the image, with no added elements or changes. The cammera doesn't move, zoom or focus in any other areas of the room.";

            // Build multipart/form-data request matching Python code exactly
            using var formData = new MultipartFormDataContent();
            
            // Add all form fields as strings (matching Python data dict)
            formData.Add(new StringContent(prompt), "prompt");
            formData.Add(new StringContent("480"), "height");
            formData.Add(new StringContent("854"), "width");
            formData.Add(new StringContent("5"), "n_seconds");
            formData.Add(new StringContent("1"), "n_variants");
            formData.Add(new StringContent("sora"), "model");
            
            // Add inpaint_items as JSON string (for image-to-video)
            // Configure which parts of the image stay consistent throughout the video
            // 
            // OPTION 1: Keep ENTIRE scene consistent (starts, middle, and ends same)
            // Use crop_bounds with full frame (0.0 to 1.0) for first and last frame
            // This constrains Sora to keep the background/tree consistent
            //
            // OPTION 2: Keep only BACKGROUND consistent (tree/room stays same, Santa can appear)
            // Use crop_bounds to specify which region should stay unchanged
            // Example: bottom_fraction = 0.7 means keep bottom 70% of image consistent
            //
            // OPTION 3: Keep specific AREA consistent (e.g., just the tree area)
            // Use left/right/top/bottom fractions to define the exact region
            //
            // Currently using OPTION 1: Full frame consistency at start and end
            var inpaintItems = new[]
            {
                // First frame (beginning of video) - entire scene
                new
                {
                    frame_index = 0,
                    type = "image",
                    file_name = fileName,
                    crop_bounds = new
                    {
                        left_fraction = 0.0,   // Keep entire width
                        top_fraction = 0.0,    // Keep entire height
                        right_fraction = 1.0,
                        bottom_fraction = 1.0
                    }
                }
                // Last frame (end of video) - return to original scene
                , new
                {
                    frame_index = 5,
                    type = "image",
                    file_name = fileName,
                    crop_bounds = new
                    {
                        left_fraction = 0.0,
                        top_fraction = 0.0,
                        right_fraction = 1.0,
                        bottom_fraction = 1.0
                    }
                }
                
                // ADVANCED: Uncomment to add middle frame constraints
                // This keeps the background consistent throughout the ENTIRE video
                // but allows foreground (Santa) to animate
                
                // Middle frame example (frame 50% through video)
                // Constrains background/tree area only (bottom 60% of frame)
                /*
                ,
                new
                {
                    frame_index = 50,  // Percentage or frame number
                    type = "image",
                    file_name = fileName,
                    crop_bounds = new
                    {
                        left_fraction = 0.0,
                        top_fraction = 0.4,    // Start at 40% from top (keep bottom 60%)
                        right_fraction = 1.0,
                        bottom_fraction = 1.0  // Keep tree/floor area consistent
                    }
                }
                */
            };
            formData.Add(new StringContent(JsonSerializer.Serialize(inpaintItems)), "inpaint_items");
            
            // Add image file (matching Python files format)
            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
            formData.Add(imageContent, "files", fileName);

            // Construct proper Sora API URL (matching Python)
            var apiUrl = $"{_endpoint.TrimEnd('/')}/openai/v1/video/generations/jobs?api-version=preview";
            
            Console.WriteLine("⏳ Sending request to Azure AI Foundry Sora...");
            Console.WriteLine($"   Endpoint: {apiUrl}");
            
            var response = await _httpClient.PostAsync(apiUrl, formData);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Error: {response.StatusCode}");
                Console.WriteLine($"Details: {errorContent}");
                return;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Full response JSON: {responseJson}");
            
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (result.TryGetProperty("id", out var jobId))
            {
                Console.WriteLine($"✓ Job created: {jobId.GetString()}");
                await PollJobStatus(jobId.GetString()!);
            }
            else
            {
                Console.WriteLine($"⚠ Unexpected response format. Full response:");
                Console.WriteLine(responseJson);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private async Task PollJobStatus(string jobId)
    {
        var statusUrl = $"{_endpoint.TrimEnd('/')}/openai/v1/video/generations/jobs/{jobId}?api-version=preview";
        var maxAttempts = 120; // 10 minutes max
        var attempt = 0;
        string? status = null;

        Console.WriteLine("\n⏳ Generating video (this may take a few minutes)...");

        while (status != "succeeded" && status != "failed" && status != "cancelled" && attempt < maxAttempts)
        {
            await Task.Delay(5000);
            attempt++;

            var response = await _httpClient.GetAsync(statusUrl);
            var responseJson = await response.Content.ReadAsStringAsync();
            var statusResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (statusResponse.TryGetProperty("status", out var statusValue))
            {
                status = statusValue.GetString();
                Console.Write($"\r   Job status: {status} ({attempt * 5}s elapsed)");
                
                if (status == "succeeded")
                {
                    Console.WriteLine("\n✓ Video generation completed!");
                    
                    // Retrieve generated video
                    if (statusResponse.TryGetProperty("generations", out var generations) && 
                        generations.GetArrayLength() > 0)
                    {
                        var firstGeneration = generations[0];
                        if (firstGeneration.TryGetProperty("id", out var generationId))
                        {
                            await DownloadGeneratedVideo(generationId.GetString()!);
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ No generations found in job result.");
                    }
                    return;
                }
                else if (status == "failed" || status == "cancelled")
                {
                    Console.WriteLine($"\n❌ Job {status}.");
                    if (statusResponse.TryGetProperty("error", out var error))
                    {
                        Console.WriteLine($"Error: {error}");
                    }
                    return;
                }
            }
        }

        Console.WriteLine("\n⏰ Timeout waiting for video generation.");
    }

    private async Task DownloadGeneratedVideo(string generationId)
    {
        try
        {
            var videoUrl = $"{_endpoint.TrimEnd('/')}/openai/v1/video/generations/{generationId}/content/video?api-version=preview";
            
            Console.WriteLine($"\n📥 Downloading video from generation: {generationId}");
            
            var videoBytes = await _httpClient.GetByteArrayAsync(videoUrl);
            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"santa_video_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
            
            await File.WriteAllBytesAsync(outputPath, videoBytes);
            
            Console.WriteLine($"\n✅ SUCCESS! Video saved to: {outputPath}");
            Console.WriteLine($"   File size: {videoBytes.Length / 1024 / 1024:F2} MB");
            Console.WriteLine($"\n🎄 Your magical Santa video is ready!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error downloading video: {ex.Message}");
        }
    }
}

