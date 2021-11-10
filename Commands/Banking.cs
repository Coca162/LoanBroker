using LoanBroker.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Shared.Models;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoanBroker.Commands;
public class Banking : BaseCommandModule
{
    public BrokerWebContext db { private get; set; }

    // someone needs to add checks if they lack a brokeraccount

    [Command("account")]
    public async Task AccountCommand(CommandContext ctx)
    {

        BrokerAccount account = await db.BrokerAccounts.FirstOrDefaultAsync(x => x.DiscordId == ctx.User.Id);

        Deposit deposit = await db.Deposits.FirstOrDefaultAsync(x => x.SVID == account.SVID);

        DiscordEmbedBuilder Embed = new();

        Embed.Title = $"{ctx.User.Username}'s account";

        Embed.AddField("Deposited", $"{deposit.Amount}");
        Embed.AddField("Lent Out", $"{await account.GetLentOut(db)}");
        Embed.AddField("Left Over", $"{await account.GetDepositLeft(db)}");
        Embed.AddField("Interest Rate", $"{Math.Round(deposit.Interest * 100, 2)}%");

        ctx.RespondAsync(Embed);
    }

    [Command("setinterestrate")]
    public async Task Setinterestrate(CommandContext ctx, decimal rate)
    {
        BrokerAccount account = await db.BrokerAccounts.FirstOrDefaultAsync(x => x.DiscordId == ctx.User.Id);

        Deposit deposit = await db.Deposits.FirstOrDefaultAsync(x => x.SVID == account.SVID);

        // we expect people to enter it like
        // setinterestrate 1.5

        deposit.Interest = rate/100;

        await db.SaveChangesAsync();

        ctx.RespondAsync($"Successfully setted your interest rate to {deposit.Interest*100}%.");
    }
}