using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Diagnostics;
using Humanizer;

namespace LoanBroker.Commands;
public class Misc : BaseCommandModule
{
    [Command("kill"), Hidden()]
    [Description("Kills the bot incase of a emergency. Coca only command for obvious reasons!")]
    public async Task Kill(CommandContext ctx)
    {
        if (ctx.User.Id == 388454632835514380) Environment.Exit(666);
    }

    [Command("ping")]
    [Description("pong!")]
    public async Task Ping(CommandContext ctx)
    {
        await ctx.RespondAsync(ctx.Client.Ping.ToString() + " ms");
    }

    [Command("uptime")]
    [Description("existence")]
    public async Task Uptime(CommandContext ctx)
    {
        TimeSpan time = DateTime.Now - Process.GetCurrentProcess().StartTime;
        await ctx.RespondAsync("Uptime: " + time.Humanize(2));
    }
}