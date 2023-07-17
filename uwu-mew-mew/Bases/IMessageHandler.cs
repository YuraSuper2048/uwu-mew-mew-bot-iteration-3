using Discord.WebSocket;

namespace uwu_mew_mew.Bases;

public interface IMessageHandler
{
    Task InitializeAsync();

    Task HandleMessageAsync(SocketUserMessage message);
}