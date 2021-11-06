using LoanBroker.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Shared.Models;

namespace LoanBroker.Commands;
public class TOS : BaseCommandModule
{
    public BrokerWebContext db { private get; set; }

    List<ulong> ReadTOS = new();

    [Command("tos"), Priority(1)]
    [Description("Accepting the TOS")]
    public async Task AcceptTOS(CommandContext ctx, string acceptance)
    {
        if (!ReadTOS.Contains(ctx.User.Id))
        {
            ctx.RespondAsync("You first need to read the TOS!\npoopfartsus");
            ReadTOS.Add(ctx.User.Id);
            return;
        }

        if (acceptance != "accept")
        {
            TOSMessage(ctx);
            return;
        }

        User? user = new User();

        try
        {
            user = db.Users.Select(x => new User() { SVID = x.SVID, Discord = x.Discord }).SingleOrDefault(x => x.Discord == ctx.User.Id);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }


        if (user == default)
        {
            ctx.RespondAsync("You do not have a CocaBot account! Do `c/register` first!");
            return;
        }


        try
        {
            db.BrokerAccounts.Add(new BrokerAccount()
            {
                SVID = user.SVID,
                MaxLoan = 2000
            });
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
        }

        await db.SaveChangesAsync();

        ctx.RespondAsync("TOS Accepted!");
    }

    [Command("tos"), Priority(0)]
    public async Task TOSMessage(CommandContext ctx)
    {
        if (db.Users.SingleOrDefault(x => x.Discord == ctx.User.Id) == default)
        {
            ctx.RespondAsync("You do not have a CocaBot account! Do `c/register` first!");
            return;
        }

        ReadTOS.Add(ctx.User.Id);
        ctx.RespondAsync("TOS: poopfartsus\nDo `b/tos accept` to accept the TOS");
    }
}