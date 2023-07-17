using System.Diagnostics;
using System.Reactive;
using System.Text;
using System.Xml.Linq;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;
using uwu_mew_mew.Misc;
using uwu_mew_mew.Openai;

namespace uwu_mew_mew;

public static class Bot
{
    public static DiscordSocketClient Client { get; private set; }

    private static int connectionCount;
    
    public static async Task RunAsync()
    {
        var config = new DiscordSocketConfig();
        config.GatewayIntents = GatewayIntents.AllUnprivileged
                                | GatewayIntents.MessageContent
                                | GatewayIntents.GuildMembers;
        Client = new DiscordSocketClient(config);
        
        var messageHandler = new MainMessageHandler();
        messageHandler.Install(Client);
        var slashCommandHandler = new MainSlashCommandHandler();
        slashCommandHandler.Install(Client);
        EncryptedJsonConvert.ComputeKey();
        
        Client.Ready += Ready;

        await Client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_AUTH_TOKEN"));
        await Client.StartAsync();

        await Client.SetStatusAsync(UserStatus.Online);
        
        Console.Write("Loading... ");

        await Task.Delay(-1);
    }

    private static Timer? statusTimer;

    private static async Task Ready()
    {
        connectionCount++;
        if (connectionCount > 5)
        {
            var multiplier = Math.Max(0d, connectionCount / 5);
            statusTimer = new Timer(CheckStatus, null, TimeSpan.FromSeconds(30 * multiplier), TimeSpan.FromSeconds(30 * multiplier));
        }
        if (connectionCount > 100)
        {
            Console.WriteLine("More than 100 connections per session. Something is not right at all.");
            Process.GetCurrentProcess().Kill();
            return;
        }
        Console.WriteLine($"Ready.\nLogged in as {Client.CurrentUser.Username} at {DateTime.Now}");

        statusTimer?.Dispose();
        statusTimer = new Timer(CheckStatus, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        
        var command = Console.ReadLine()!;

        if (command == "tfui")
        {
            await DumpAllAsync();
        }
        if (command == "aagb")
        {
            Debugger.Break();
        }
    }

    private static void CheckStatus(object? state)
    {
        CheckStatusAsync();
    }

    private static async Task CheckStatusAsync()
    {
        if (Client.ConnectionState == ConnectionState.Connected) return;
        
        await Task.Delay(TimeSpan.FromSeconds(30));
        
        if (Client.ConnectionState == ConnectionState.Connected) return;
        
        Console.Write("Down for 30 seconds, reconnecting... ");
        
        await Client.LogoutAsync();
        await Client.StopAsync();
            
        var config = new DiscordSocketConfig();
        config.GatewayIntents = GatewayIntents.AllUnprivileged
                                | GatewayIntents.MessageContent
                                | GatewayIntents.GuildMembers;
        Client = new DiscordSocketClient(config);
        
        var messageHandler = new MainMessageHandler();
        messageHandler.Install(Client);
        var slashCommandHandler = new MainSlashCommandHandler();
        slashCommandHandler.Install(Client);
        EncryptedJsonConvert.ComputeKey();
        
        Client.Ready += Ready;

        await Client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_AUTH_TOKEN"));
        await Client.StartAsync();

        await Client.SetStatusAsync(UserStatus.Online);
    }

    private static async Task DumpAllAsync()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var count = 0;

        var guilds = new IGuild[]
        {
            Client.Rest.GetGuildAsync(1091312558906019913).Result,
            Client.Rest.GetGuildAsync(1105337590837678120).Result,
            Client.Rest.GetGuildAsync(857596374635642931).Result,
        };

        Parallel.ForEach(guilds, async g => await ProcessGuild(g));

        async Task ProcessGuild(IGuild guild)
        {
            var channels = await guild.GetTextChannelsAsync();

            Parallel.ForEach(channels, async c => await ProcessChannel((IRestMessageChannel)c));

            async Task ProcessChannel(IRestMessageChannel channel)
            {
                try
                {
                    await channel.GetMessagesAsync(1).FirstAsync();
                }
                catch
                {
                    return;
                }
                
                await Task.Delay(1000);

                async Task ProcessBatch(IReadOnlyCollection<IMessage> messages)
                {
                    void ProcessMessage(IMessage currentMessage)
                    {
                        try
                        {
                            MessageDatabase.AddMessage(currentMessage);

                            count++;
                            Console.Write($"| {count,7} | {MathF.Round(count / (stopwatch.ElapsedMilliseconds / 1000), 2),5} msg/s | {MathF.Round(stopwatch.ElapsedMilliseconds / 1000),3} s |\r");
                        }
                        catch
                        {
                            //whatever i dont care
                        }
                    }

                    Parallel.ForEach(messages, ProcessMessage);
                }
                
                var messagesAsync = channel.GetMessagesAsync(int.MaxValue);

                try
                {
                    await foreach (var messages in messagesAsync) ProcessBatch(messages);
                }
                catch
                {
                    //fc it
                }
            }
                
        }
    }
}