using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🎅 Santa Video Generator - Powered by Azure AI Foundry Sora");
        Console.WriteLine("============================================================\n");

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var endpoint = config["AzureAI:Endpoint"];
        var apiKey = config["AzureAI:ApiKey"];
        var deploymentName = config["AzureAI:DeploymentName"];

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(deploymentName))
        {
            Console.WriteLine("❌ Configuration missing! Please run provision-sora-model.ps1 first.");
            return;
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

        var videoGenerator = new SantaVideoGenerator(endpoint, apiKey, deploymentName);
        await videoGenerator.GenerateSantaVideo(imagePath);
    }
}

public class SantaVideoGenerator
{
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _deploymentName;
    private readonly HttpClient _httpClient;

    public SantaVideoGenerator(string endpoint, string apiKey, string deploymentName)
    {
        _endpoint = endpoint;
        _apiKey = apiKey;
        _deploymentName = deploymentName;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
    }

    public async Task GenerateSantaVideo(string imagePath)
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

            var prompt = "Santa Claus magically appears in the Christmas scene, walks gracefully to the Christmas tree with a bag of presents, carefully places beautifully wrapped gifts underneath the tree, steps back to admire his work with a warm smile, waves goodbye, and disappears in a shower of festive sparkles and twinkling lights. The scene is warm, magical, and filled with holiday spirit.";

            // Build multipart/form-data request matching Python code exactly
            using var formData = new MultipartFormDataContent();
            
            // Add all form fields as strings (matching Python data dict)
            formData.Add(new StringContent(prompt), "prompt");
            formData.Add(new StringContent("720"), "height");
            formData.Add(new StringContent("1280"), "width");
            formData.Add(new StringContent("10"), "n_seconds");
            formData.Add(new StringContent("1"), "n_variants");
            formData.Add(new StringContent(_deploymentName), "model");
            
            // Add inpaint_items as JSON string (for image-to-video)
            var inpaintItems = new[]
            {
                new
                {
                    frame_index = 0,
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

