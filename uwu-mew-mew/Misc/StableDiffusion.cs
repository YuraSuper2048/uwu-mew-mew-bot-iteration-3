using System.Net.Http.Headers;
using System.Text;
using Google.Cloud.Storage.V1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace uwu_mew_mew.Misc;

public class StableDiffusion
{
    public record struct GenerationResult(byte[] image, long seed);
    
    public const double DefaultCfg = 5.5;
    public const int DefaultSteps = 50;
    
    public static async Task<GenerationResult> GenerateImage(string prompt, double cfgScale = DefaultCfg, int steps = DefaultSteps, long seed = -1)
    {
        try
        {
            var payload = new
            {
                prompt,
                negative_prompt = "nsfw, naked, lewd",
                steps,
                cfg_scale = cfgScale,
                sampler_name = "DPM++ 2M SDE Karras",
                seed
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("SD_CREDENTIALS")!));
            var request = new HttpRequestMessage(HttpMethod.Post, txt2imgHolyshitUrl);
            request.Content = httpContent;
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
            var response = await GlobalHttpClient.Instance.SendAsync(request);

            if (!response.IsSuccessStatusCode) return new GenerationResult(Array.Empty<byte>(), -1);
        
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            var image = Convert.FromBase64String(((JArray)result["images"]!).First().Value<string>()!);

            return new GenerationResult(image, (long)JObject.Parse(result["info"]!.Value<string>()!)["seed"]!);
        }
        catch
        {
            return new GenerationResult(Array.Empty<byte>(), -1);
        }
    }

    public static string Upload(byte[] image)
    {
        using var uploadStream = new MemoryStream(image);

        var storageClient = StorageClient.Create();
        var obj = storageClient.UploadObject("uwu-mew-mew", $"{Guid.NewGuid()}.png", "image/png", uploadStream);
        return obj.MediaLink;
    }
    
    private const string txt2imgHolyshitUrl = "http://127.0.0.1:7860/sdapi/v1/txt2img";
}