using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Discord;
using Discord.WebSocket;
using Google.Cloud.Storage.V1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using uwu_mew_mew.Bases;
using uwu_mew_mew.Misc;
using uwu_mew_mew.Openai;
using OpenAi = uwu_mew_mew.Openai.OpenAi;
// ReSharper disable InconsistentlySynchronizedField

namespace uwu_mew_mew.Handlers;

public partial class AiHandler : IMessageHandler, ISlashCommandHandler
{
    private EmbedBuilder GetEmbed(IUser user)
    {
        return new EmbedBuilder()
            .WithTitle($"{Chats[user.Id].System} with GPT4")
            .WithThumbnailUrl(Chats[user.Id].System switch
            {
                SystemPromptType.sbgpt => "https://storage.googleapis.com/uwu-mew-mew/sbGPT.png",
                SystemPromptType.sbgpt_full => "https://storage.googleapis.com/uwu-mew-mew/sbGPT.png",
                SystemPromptType.chatgpt => "https://storage.googleapis.com/uwu-mew-mew/chatgpt.png",
                _ => throw new ArgumentOutOfRangeException()
            })
            .WithColor(255, 192, 203)
            .WithAuthor(EmbedHelper.GetEmbedAuthorFromUser(user))
            .WithCurrentTimestamp();
    }

    private string GetSystemPrompt(SocketUser user)
    {
        var system = File.ReadAllText($"{Chats[user.Id].System}.txt")
            .Replace("$user_ping", user.Mention)
            .Replace("$bot_ping", Bot.Client.CurrentUser.Mention)
            .Replace("$username", user.Username);
        return system;
    }

    public async Task HandleMessageAsync(SocketUserMessage message)
    {
        if(message.Author.IsBot)
            return;
        
        // ReSharper disable once SimplifyLinqExpressionUseAll
        if(!message.MentionedUsers.Any(u => u.Id == Bot.Client.CurrentUser.Id))
            return;

        var authorId = message.Author.Id;
        var chat = Chats.GetOrAdd(authorId, new ChatData(new()));

        var aiMessage = await message.ReplyAsync("thinking uwu...", embed: GetEmbed(message.Author).Build());

        if (Chats[authorId].CurrentGeneration is not null) await aiMessage.ModifyAsync(m => m.Embed = GetEmbed(message.Author).WithDescription("**WAITING**").Build());
        while (Chats[authorId].CurrentGeneration is not null) await Task.Delay(100);
        Chats[authorId] = Chats[authorId] with { CurrentGeneration = message.Id };
        
        var streamTask = HandleStreaming(message, chat, aiMessage, authorId);
        await Task.WhenAny(streamTask, Task.Delay(TimeSpan.FromMinutes(3)));
        
        SaveChats();
        Chats[authorId] = Chats[authorId] with { CurrentGeneration = null };

        await aiMessage.ModifyAsync(m => m.Content = $"{aiMessage.Content}\nuwu done~");
        await aiMessage.ModifyAsync(m => m.Components = new ComponentBuilder().WithButton("Reset", "reset", ButtonStyle.Secondary, Emoji.Parse(":x:")).WithButton("Character", "character", ButtonStyle.Secondary, Emoji.Parse(":cat:")).Build());
    }

    private async Task HandleStreaming(SocketUserMessage message, ChatData chat, IUserMessage aiMessage, ulong authorId)
    {
        var userMessage = message.Content.Replace($"<@{Bot.Client.CurrentUser.Id}>", "").Trim();

        var messages = chat.Messages;
        messages.Add(new("system", GetSystemPrompt(message.Author)));
        messages.Add(new("user", userMessage));

        Stream:
        var result = OpenAi.Chat.StreamChatCompletionAsync(messages, model: "gpt-4", functions: _functions);

        var streamBuilder = new StringBuilder();
        var functionArgumentBuilder = new StringBuilder();
        var functionNameBuilder = new StringBuilder();
        var stopwatch = Stopwatch.StartNew();

        async Task UpdateMessage()
        {
            var newEmbed = aiMessage.Embeds.First().ToEmbedBuilder()
                .WithDescription(streamBuilder.ToString())
                .WithFooter($"{messages.Count} messages");
            await aiMessage.ModifyAsync(m => m.Embed = newEmbed.Build());
        }

        await foreach (var data in result)
        {
            if (data.FinishReason == "length" && streamBuilder.ToString().Trim() == string.Empty)
            {
                messages.RemoveAt(1);
                goto Stream;
            }
            if (data.FinishReason == "function_call")
            {
                await aiMessage.ModifyAsync(m => m.Content = $"{aiMessage.Content}\nWorking on {functionNameBuilder} with arguments {JObject.Parse(functionArgumentBuilder.ToString()).ToString(Formatting.None)}...");

                var functionCall = new JObject(
                    new JProperty("name", functionNameBuilder.ToString()),
                    new JProperty("arguments", functionArgumentBuilder.ToString()
                    ));

                messages.Add(new("assistant", streamBuilder.ToString(), function_call: functionCall));

                var functionResult = await HandleFunctionCall(functionNameBuilder.ToString(), functionArgumentBuilder.ToString(), aiMessage);

                messages.Add(new("function", functionResult, functionNameBuilder.ToString()));

                await aiMessage.ModifyAsync(m => m.Content = $"{aiMessage.Content}\nDone working on {functionNameBuilder}.");

                goto Stream;
            }

            if (data.FinishReason != "") break;

            if (data.FunctionCall != null)
            {
                var functionCallData = JObject.Parse(data.FunctionCall);
                if (functionCallData["arguments"] != null) functionArgumentBuilder.Append(functionCallData["arguments"]!.Value<string>());
                if (functionCallData["name"] != null) functionNameBuilder.Append(functionCallData["name"]!.Value<string>());
            }

            streamBuilder.Append(data.Content);

            if (stopwatch.ElapsedMilliseconds > 250)
            {
                await UpdateMessage();
                stopwatch.Restart();
            }
        }

        messages.Add(new("assistant", streamBuilder.ToString()));

        messages.RemoveAt(0);
        chat = chat with { Messages = messages };
        Chats[authorId] = chat;

        await UpdateMessage();
    }

    private static void SaveChats()
    {
        lock (Chats)
        {
            var data = EncryptedJsonConvert.Serialize(Chats);
            File.WriteAllBytes("data/chats.bin", data);
        }
    }
    
    static AiHandler()
    {
        try
        {
            var data = File.ReadAllBytes("data/chats.bin");
            var chats = EncryptedJsonConvert.Deserialize<ConcurrentDictionary<ulong, ChatData>>(data);

            Chats = chats ?? new();
        }
        catch
        {
            Chats = new();
        }
    }

    private static readonly ConcurrentDictionary<ulong, ChatData> Chats;

    private record ChatData(List<OpenAi.Chat.ChatMessage> Messages, SystemPromptType System = SystemPromptType.sbgpt, [property: JsonIgnore] ulong? CurrentGeneration = null);
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    public enum SystemPromptType
    {
        sbgpt,
        sbgpt_full,
        chatgpt
    }
}