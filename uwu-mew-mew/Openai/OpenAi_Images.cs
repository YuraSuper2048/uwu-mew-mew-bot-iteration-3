using System.Dynamic;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using uwu_mew_mew.Misc;

namespace uwu_mew_mew.Openai;

public static partial class OpenAi
{
    public static class Images
    {
        public static async Task<string> GenerateImage(string prompt)
        {
            dynamic requestBody = new ExpandoObject();
            requestBody.prompt = prompt;

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{Endpoint}/images/generations");
            request.Content = content;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Key);

            var response = await GlobalHttpClient.Instance.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = JObject.Parse(await response.Content.ReadAsStringAsync());
            return responseBody["data"]![0]!["url"]!.Value<string>()!;
        }
    }
}