using Discord.WebSocket;

namespace uwu_mew_mew.Bases;

public interface ISlashCommandHandler
{
    Task RegisterSlashCommands();

    Task HandleSlashCommandAsync(SocketSlashCommand arg);
}