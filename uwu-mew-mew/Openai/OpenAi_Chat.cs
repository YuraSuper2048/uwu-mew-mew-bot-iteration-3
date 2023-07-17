using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using uwu_mew_mew.Misc;

namespace uwu_mew_mew.Openai;

public static partial class OpenAi
{
    public static class Chat
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public record ChatMessage(string role, string content, string name = "", JObject? function_call = null);

        public struct ChatResult
        {
            public JObject Raw;
            public string? Content;
            public string? FinishReason;
            public string? FunctionCall;
        }

        public static async Task<ChatResult> GetChatCompletionAsync(IEnumerable<ChatMessage> messages,
            string model = "gpt-3.5-turbo",
            IReadOnlyList<JObject>? functions = null)
        {
            dynamic requestBody = new ExpandoObject();
            requestBody.model = model;
            requestBody.temperature = 0;
            requestBody.messages = new List<ExpandoObject>();
            foreach (var chatMessage in messages)
            {
                dynamic message = new ExpandoObject();
                message.role = chatMessage.role;
                message.content = chatMessage.content;
                if (chatMessage.function_call != null)
                    message.function_call = chatMessage.function_call;
                if (chatMessage.name != string.Empty)
                    message.name = chatMessage.name;
                requestBody.messages.Add(message);
            }

            if (functions is not null)
            {
                requestBody.functions = functions;
                requestBody.function_call = "auto";
            }

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{Endpoint}/chat/completions");
            request.Content = content;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Key);

            var response = await GlobalHttpClient.Instance.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = JObject.Parse(await response.Content.ReadAsStringAsync());
            return new ChatResult
            {
                Raw = responseBody,
                Content = responseBody["choices"]?[0]?["message"]?["content"]?.ToString(),
                FinishReason = responseBody["choices"]?[0]?["finish_reason"]?.ToString(),
                FunctionCall = responseBody["choices"]?[0]?["message"]?["function_call"]?.ToString()
            };
        }

        public static async IAsyncEnumerable<ChatResult> StreamChatCompletionAsync(IEnumerable<ChatMessage> messages,
            string model = "gpt-3.5-turbo",
            IReadOnlyList<JObject>? functions = null)
        {
            dynamic requestBody = new ExpandoObject();
            requestBody.model = model;
            requestBody.messages = new List<ExpandoObject>();
            requestBody.stream = true;
            foreach (var chatMessage in messages)
            {
                dynamic message = new ExpandoObject();
                message.role = chatMessage.role;
                message.content = chatMessage.content;
                if (chatMessage.function_call != null)
                    message.function_call = chatMessage.function_call;
                if (chatMessage.name != string.Empty)
                    message.name = chatMessage.name;
                requestBody.messages.Add(message);
            }

            if (functions is not null)
            {
                requestBody.functions = functions;
                requestBody.function_call = "auto";
            }

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            SendRequest:
            var request = new HttpRequestMessage(HttpMethod.Post, $"{Endpoint}/chat/completions");
            request.Content = content;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Key);

            var response = await GlobalHttpClient.Instance.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (response.StatusCode == HttpStatusCode.BadGateway)
                goto SendRequest;

            var stream = await response.Content.ReadAsStreamAsync();
            var reader = new StreamReader(stream);

            var gotAnyResponse = false;

            while (await reader.ReadLineAsync() is { } line)
            {
                if (!line.ToLower().StartsWith("data:")) continue;

                var dataString = line[5..].Trim();

                if (dataString == "[DONE]")
                {
                    if(!gotAnyResponse)
                        goto SendRequest;
                    
                    continue;
                }

                var data = JObject.Parse(dataString);
                
                if(data["choices"]?[0]?["delta"]?["content"]?.ToString() != string.Empty 
                   && data["choices"]?[0]?["delta"]?["content"]?.ToString() != null)
                    gotAnyResponse = true;

                yield return new ChatResult
                {
                    Raw = data,
                    Content = data["choices"]?[0]?["delta"]?["content"]?.ToString(),
                    FinishReason = data["choices"]?[0]?["finish_reason"]?.ToString(),
                    FunctionCall = data["choices"]?[0]?["delta"]?["function_call"]?.ToString()
                };
            }
            
            reader.Dispose();
        }
    }
}