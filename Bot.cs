using DSharpPlus;
using DSharpPlus.CommandsNext;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Timers;
using SpookVooper.Api;
using SpookVooper.Api.Entities;
using DSharpPlus.Entities;
using static Shared.Main;

public class Bot
{
    public static DiscordClient Client { get; private set; }
    public static CommandsNextExtension Commands { get; private set; }
    public static async Task RunAsync(Config ConfigJson)
    {
        DiscordConfiguration config = new()
        {
            Token = ConfigJson.Token,
            TokenType = TokenType.Bot,
            MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug
        };

        Client = new DiscordClient(config);
        Client.Ready += OnClientReady;

        CommandsNextConfiguration commandsConfig = new()
        {
            StringPrefixes = ConfigJson.Prefix,
            Services = new ServiceCollection().AddDbContextPool<BrokerWebContext>((serviceProvider, options) =>
            {
                options.UseMySql(BrokerWebContext.ConnectionString, BrokerWebContext.version);
            }).BuildServiceProvider()
        };

        Commands = Client.UseCommandsNext(commandsConfig);

        Commands.SetHelpFormatter<HelpFormatter>();

        Commands.RegisterCommands(Assembly.GetExecutingAssembly());

        await Client.ConnectAsync();
    }

    private static async Task OnClientReady(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs e)
    {
        Console.WriteLine("LoanBroker on!");
    }
}
