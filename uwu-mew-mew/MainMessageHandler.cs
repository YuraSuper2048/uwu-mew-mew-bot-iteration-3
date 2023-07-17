using Discord.WebSocket;
using uwu_mew_mew.Bases;
using uwu_mew_mew.Handlers;
using uwu_mew_mew.Misc;
using uwu_mew_mew.Openai;

namespace uwu_mew_mew;

public class MainMessageHandler
{
    private IMessageHandler[] handlers = 
    {
        new AiHandler()
    };
    
    public void Install(DiscordSocketClient client)
    {
        foreach (var messageHandler in handlers)
        {
            messageHandler.InitializeAsync();
        }

        client.MessageReceived += HandleMessage;
    }
    
    private Task HandleMessage(SocketMessage socketMessage)
    {
        if (socketMessage is not SocketUserMessage message)
            return Task.CompletedTask;
        MessageDatabase.AddMessage(message);
        
        foreach (var messageHandler in handlers)
        {
            messageHandler.HandleMessageAsync(message);
        }
        
        return Task.CompletedTask;
    }
}