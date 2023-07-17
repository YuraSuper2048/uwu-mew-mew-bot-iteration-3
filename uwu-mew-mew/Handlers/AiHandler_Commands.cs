using Discord;
using Discord.WebSocket;

namespace uwu_mew_mew.Handlers;

public partial class AiHandler
{
    public async Task InitializeAsync()
    {
        Bot.Client.SelectMenuExecuted += SelectMenuPressed;
        Bot.Client.ButtonExecuted += ButtonExecuted;
    }

    private async Task ButtonExecuted(SocketMessageComponent arg)
    {
        if (arg.Data.CustomId == "reset")
        {
            Reset(arg.User.Id);
            await arg.RespondAsync("owo i forgor... :crying_cat_face:", ephemeral: true);
        }
        else if(arg.Data.CustomId == "character")
        {
            await arg.RespondAsync("uwu choose wisely~", components: SystemPromptMenu.Build(), ephemeral: true);
        }
    }

    private async Task SelectMenuPressed(SocketMessageComponent arg)
    {
        if (arg.Data.CustomId == "system")
        {
            var userId = arg.User.Id;
            Chats[userId] = Chats[userId] with { System = Enum.Parse<SystemPromptType>(arg.Data.Values.First()) };
            SaveChats();
            await arg.RespondAsync("done uwu~", ephemeral: true);
        }
    }

    public async Task RegisterSlashCommands()
    {
        var reset = new SlashCommandBuilder();
        reset.Name = "uwureset";
        reset.Description = "Resets the conversation with uwu mew mew";
        var character = new SlashCommandBuilder();
        character.Name = "uwucharacter";
        character.Description = "Changes the system prompt of uwu mew mew";

        var commands = await Bot.Client.GetGlobalApplicationCommandsAsync();
        if (!commands.Any(c => c.Name == reset.Name))
            await Bot.Client.CreateGlobalApplicationCommandAsync(reset.Build());
        if (!commands.Any(c => c.Name == character.Name))
            await Bot.Client.CreateGlobalApplicationCommandAsync(character.Build());
    }

    public async Task HandleSlashCommandAsync(SocketSlashCommand arg)
    {
        if (arg.Data.Name == "uwureset")
        {
            Reset(arg.User.Id);
            await arg.FollowupAsync("N-nya?.. M-mastew?..");
        }
        else if (arg.Data.Name == "uwucharacter")
        {
            await arg.FollowupAsync("Uwu mew~", components: SystemPromptMenu.Build());
        }
    }

    private void Reset(ulong user)
    {
        Chats[user] = Chats[user] with { Messages = new() };
        SaveChats();
    }
    

    private static ComponentBuilder SystemPromptMenu
    {
        get
        {
            var menu = new SelectMenuBuilder()
                .WithMinValues(1).WithMaxValues(1)
                .WithPlaceholder("Select character")
                .WithCustomId("system")
                .AddOption("sbGPT simple", "sbgpt", "Default. Catgirl prompt without unnecessary fluff")
                .AddOption("sbGPT 2", "sbgpt_full", "Catgirl prompt with a jailbreak")
                .AddOption("ChatGPT", "chatgpt", "Classic ChatGPT without a jailbreak");
            var components = new ComponentBuilder()
                .WithSelectMenu(menu);
            return components;
        }
    }
}