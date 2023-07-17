using Discord.WebSocket;
using uwu_mew_mew.Bases;
using uwu_mew_mew.Handlers;

namespace uwu_mew_mew;

public class MainSlashCommandHandler
{
    private ISlashCommandHandler[] handlers = 
    {
        new AiHandler(),
        new ImageGenerationHandler()
    };
    
    public void Install(DiscordSocketClient client)
    {
        client.SlashCommandExecuted += HandleSlashCommand;
        client.Ready += ClientReady;
    }

    private async Task ClientReady()
    {
        foreach (var slashCommandHandler in handlers)
        {
            slashCommandHandler.RegisterSlashCommands();
        }
    }

    private Task HandleSlashCommand(SocketSlashCommand arg)
    {
        arg.DeferAsync();

        foreach (var slashCommandHandler in handlers)
        {
            slashCommandHandler.HandleSlashCommandAsync(arg);
        }
        
        return Task.CompletedTask;
    }
}