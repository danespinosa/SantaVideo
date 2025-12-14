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
            var base64Image = Convert.ToBase64String(imageBytes);
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

            var requestBody = new
            {
                prompt = "Santa Claus magically appears in the Christmas scene, walks gracefully to the Christmas tree with a bag of presents, carefully places beautifully wrapped gifts underneath the tree, steps back to admire his work with a warm smile, waves goodbye, and disappears in a shower of festive sparkles and twinkling lights. The scene is warm, magical, and filled with holiday spirit.",
                image = new
                {
                    data = base64Image,
                    mimeType = mimeType
                },
                duration = 10,
                aspectRatio = "16:9",
                quality = "high",
                includeAudio = true
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var apiUrl = _endpoint;
            
            Console.WriteLine("⏳ Sending request to Azure AI Foundry Sora... {0}", apiUrl);
            var response = await _httpClient.PostAsync(apiUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Error: {response.StatusCode}");
                Console.WriteLine($"Details: {errorContent}");
                return;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (result.TryGetProperty("id", out var operationId))
            {
                Console.WriteLine($"✓ Video generation started! Operation ID: {operationId.GetString()}");
                await PollVideoGenerationStatus(operationId.GetString()!);
            }
            else if (result.TryGetProperty("videoUrl", out var videoUrl))
            {
                await DownloadVideo(videoUrl.GetString()!);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private async Task PollVideoGenerationStatus(string operationId)
    {
        var statusUrl = $"{_endpoint.TrimEnd('/')}/openai/operations/{operationId}?api-version=2024-08-01-preview";
        var maxAttempts = 60;
        var attempt = 0;

        Console.WriteLine("\n⏳ Generating video (this may take a few minutes)...");

        while (attempt < maxAttempts)
        {
            await Task.Delay(5000);
            attempt++;

            var response = await _httpClient.GetAsync(statusUrl);
            var responseJson = await response.Content.ReadAsStringAsync();
            var status = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (status.TryGetProperty("status", out var statusValue))
            {
                var statusText = statusValue.GetString();
                
                if (statusText == "succeeded")
                {
                    if (status.TryGetProperty("result", out var result) && 
                        result.TryGetProperty("videoUrl", out var videoUrl))
                    {
                        Console.WriteLine("\n✓ Video generation completed!");
                        await DownloadVideo(videoUrl.GetString()!);
                        return;
                    }
                }
                else if (statusText == "failed")
                {
                    Console.WriteLine($"\n❌ Video generation failed.");
                    if (status.TryGetProperty("error", out var error))
                    {
                        Console.WriteLine($"Error: {error}");
                    }
                    return;
                }
                else
                {
                    Console.Write($"\r   Progress: {statusText} ({attempt * 5}s elapsed)");
                }
            }
        }

        Console.WriteLine("\n⏰ Timeout waiting for video generation.");
    }

    private async Task DownloadVideo(string videoUrl)
    {
        try
        {
            Console.WriteLine($"\n📥 Downloading video from: {videoUrl}");
            
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
