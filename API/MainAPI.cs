using LoanBroker.Managers;
using LoanBroker.Models;
using LoanBroker.Models.SVModels;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;

namespace LoanBroker.API;

[EnableCors("ApiPolicy")]
public class MainAPI : BaseAPI
{
    public static void AddRoutes(WebApplication app)
    {
        app.MapGet("api/accounts/{entityid}/debt/total", GetTotalDebt).RequireCors("ApiPolicy");
        app.MapGet("api/accounts/{entityid}/creditscore", GetCreditScore).RequireCors("ApiPolicy");
        app.MapGet("api/accounts/{entityid}/maxdebtcapacity", GetMaxLoan).RequireCors("ApiPolicy");
        app.MapGet("api/accounts/{entityid}/muittobaseinterestrate", GetMuitToBaseInterestRate).RequireCors("ApiPolicy");
    }

    private static async Task<BrokerAccount> CreateAccount(BrokerContext dbctx, long entityid)
    {
        BrokerAccount account = new()
        {
            Id = entityid,
            Access_Token = "",
            MaxLoan = 0.0m,
            TrustedAccount = false,
            CreditScore = 0,
            RepaymentSetting = RepaymentSettingTypes.MaintainBalance
        };

        await account.UpdateCreditScore(dbctx);
        dbctx.Add(account);
        await dbctx.SaveChangesAsync();
        return account;
    }

    private static async Task GetMuitToBaseInterestRate(HttpContext ctx, BrokerContext dbctx, long entityid)
    {
        BrokerAccount? account = await dbctx.BrokerAccounts.FindAsync(entityid);
        if (account is null) account = await CreateAccount(dbctx, entityid);
        await account.UpdateCreditScore(dbctx);

        await ctx.Response.WriteAsync(account.GetMuitToBaseInterestRate().ToString());
    }

    private static async Task GetCreditScore(HttpContext ctx, BrokerContext dbctx, long entityid)
    {
        BrokerAccount? account = await dbctx.BrokerAccounts.FindAsync(entityid);
        if (account is null) account = await CreateAccount(dbctx, entityid);
        await account.UpdateCreditScore(dbctx);

        await ctx.Response.WriteAsync(account.CreditScore.ToString());
    }

    private static async Task GetMaxLoan(HttpContext ctx, BrokerContext dbctx, long entityid)
    {
        BrokerAccount? account = await dbctx.BrokerAccounts.FindAsync(entityid);
        if (account is null) account = await CreateAccount(dbctx, entityid);
        await account.UpdateCreditScore(dbctx);

        await ctx.Response.WriteAsync(account.MaxLoan.ToString());
    }

    private static async Task GetTotalDebt(HttpContext ctx, BrokerContext dbctx, long entityid)
    {
        BrokerAccount? account = await dbctx.BrokerAccounts.FindAsync(entityid);
        if (account is null) account = await CreateAccount(dbctx, entityid);

        await ctx.Response.WriteAsync((await dbctx.Loans.Where(x => x.IsActive).SumAsync(x => x.TotalAmount-x.PaidBack)).ToString());
    }
}
