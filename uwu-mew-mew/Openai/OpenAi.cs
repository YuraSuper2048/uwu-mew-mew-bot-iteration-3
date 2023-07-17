using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace uwu_mew_mew.Openai;

public static partial class OpenAi
{
    private static readonly string Endpoint = Environment.GetEnvironmentVariable("OPENAI_API_ENDPOINT")!;
    private static readonly string Key = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
}