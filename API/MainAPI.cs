using LoanBroker.Managers;
using LoanBroker.Models;
using LoanBroker.Models.NonDBO;
using LoanBroker.Models.SVModels;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;

namespace LoanBroker.API;

[EnableCors("ApiPolicy")]
public class MainAPI : BaseAPI
{
    private static readonly HttpClient client = new HttpClient();
    public static void AddRoutes(WebApplication app)
    {
        app.MapGet("api/accounts/{entityid}/debt/total", GetTotalDebt).RequireCors("ApiPolicy");
        app.MapGet("api/accounts/{entityid}/creditscore", GetCreditScore).RequireCors("ApiPolicy");
        app.MapGet("api/accounts/{entityid}/maxdebtcapacity", GetMaxLoan).RequireCors("ApiPolicy");
        app.MapGet("api/accounts/{entityid}/muittobaseinterestrate", GetMuitToBaseInterestRate).RequireCors("ApiPolicy");
        app.MapPost("api/deposits", CreateDeposit).RequireCors("ApiPolicy");
        app.MapGet("api/accounts/{entityid}/deposits", GetAccountDeposits).RequireCors("ApiPolicy");
        app.MapGet("api/accounts/{entityid}/totaldeposited", GetTotalDeposited).RequireCors("ApiPolicy");
        app.MapGet("api/accounts/{entityid}/amountavailabletolend", GetAmountAvailableToLend).RequireCors("ApiPolicy");
        app.MapGet("api/accounts/{entityid}/lent", GetAmountLent).RequireCors("ApiPolicy");
        app.MapGet("api/accounts/{entityid}/profitperday", GetProfitPerDay).RequireCors("ApiPolicy");
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
        DBCache.AddNew(entityid, account);

        return account;
    }

    private static async Task GetTotalDeposited(HttpContext ctx, BrokerContext dbctx, long entityid)
    {
        BrokerAccount? account = DBCache.Get<BrokerAccount>(entityid);
        if (account is null) account = await CreateAccount(dbctx, entityid);
        await account.UpdateCreditScore(dbctx);

        var amount = await dbctx.Deposits.Where(x => x.IsActive && x.AccountId == entityid).SumAsync(x => x.Amount);
        amount += await dbctx.Loaners.Include(x => x.Loan).Where(x => x.Loan.IsActive && x.LoanerAccountId == entityid).SumAsync(x => x.Loan.BaseAmount * x.Percent);
        await ctx.Response.WriteAsync(amount.ToString());
    }

    private static async Task GetProfitPerDay(HttpContext ctx, BrokerContext dbctx, long entityid)
    {
        BrokerAccount? account = DBCache.Get<BrokerAccount>(entityid);
        if (account is null) account = await CreateAccount(dbctx, entityid);
        await account.UpdateCreditScore(dbctx);

        var loaners = await dbctx.Loaners.Include(x => x.Loan).Where(x => x.Loan.IsActive && x.LoanerAccountId == entityid).ToListAsync();
        var amount = 0.0m;
        foreach (var loaner in loaners)
        {
            amount += (loaner.Loan.TotalAmount - loaner.Loan.BaseAmount) * loaner.Percent * (decimal)(loaner.Loan.End.Subtract(loaner.Loan.Start).TotalDays);
        }
        await ctx.Response.WriteAsync(amount.ToString());
    }

    private static async Task GetAmountLent(HttpContext ctx, BrokerContext dbctx, long entityid)
    {
        BrokerAccount? account = DBCache.Get<BrokerAccount>(entityid);
        if (account is null) account = await CreateAccount(dbctx, entityid);
        await account.UpdateCreditScore(dbctx);

        var amount = await dbctx.Loaners.Include(x => x.Loan).Where(x => x.Loan.IsActive && x.LoanerAccountId == entityid).SumAsync(x => x.Loan.BaseAmount * x.Percent);
        await ctx.Response.WriteAsync(amount.ToString());
    }

    private static async Task GetAmountAvailableToLend(HttpContext ctx, BrokerContext dbctx, long entityid)
    {
        BrokerAccount? account = DBCache.Get<BrokerAccount>(entityid);
        if (account is null) account = await CreateAccount(dbctx, entityid);
        await account.UpdateCreditScore(dbctx);

        var amount = await dbctx.Deposits.Where(x => x.IsActive && x.AccountId == entityid).SumAsync(x => x.Amount);
        await ctx.Response.WriteAsync(amount.ToString());
    }

    private static async Task GetAccountDeposits(HttpContext ctx, BrokerContext dbctx, long entityid)
    {
        BrokerAccount? account = DBCache.Get<BrokerAccount>(entityid);
        if (account is null) account = await CreateAccount(dbctx, entityid);
        await account.UpdateCreditScore(dbctx);

        await ctx.Response.WriteAsJsonAsync(await dbctx.Deposits.Where(x => x.IsActive && x.AccountId == entityid).OrderByDescending(x => x.TimeCreated).ToListAsync());
    }

    private static async Task CreateDeposit(HttpContext ctx, BrokerContext dbctx, string privatekey, string oauthkey, [FromBody] Deposit deposit)
    {
        BrokerAccount account = DBCache.Get<BrokerAccount>(deposit.AccountId) ?? await CreateAccount(dbctx, deposit.AccountId);
        account.Access_Token = oauthkey;
        await account.UpdateCreditScore(dbctx);

        if (privatekey != SVConfig.instance.LoanBrokerAPIKey)
        {
            await Unauthorized("", ctx);
            return;
        }

        deposit.Interest /= 100.0m;

        if (deposit.Interest <= 0.005m)
        {
            await BadRequest("Interest must be above 0.5%!", ctx);
            return;
        }

        if (deposit.Amount < 100.0m)
        {
            await BadRequest("Amount must be 100 or higher credits!", ctx);
            return;
        }

        SVTransaction tran = new(deposit.AccountId, AccountSystem.GroupSVID, deposit.Amount, oauthkey, "Deposit into VTech Loan Broker", 9);
        TaskResult result = await tran.ExecuteAsync(client);
        if (!result.Succeeded)
        {
            await BadRequest(result.Message, ctx);
            return;
        }

        deposit.TotalAmount = deposit.Amount;
        deposit.TimeCreated = DateTime.UtcNow;
        deposit.IsActive = true;
        deposit.Id = IdManagers.GeneralIdGenerator.Generate();
        dbctx.Deposits.Add(deposit);
        await dbctx.SaveChangesAsync();
        await ctx.Response.WriteAsync("Successfully deposited.");
    }

    private static async Task GetMuitToBaseInterestRate(HttpContext ctx, BrokerContext dbctx, long entityid)
    {
        BrokerAccount? account = DBCache.Get<BrokerAccount>(entityid);
        if (account is null) account = await CreateAccount(dbctx, entityid);
        await account.UpdateCreditScore(dbctx);

        await ctx.Response.WriteAsync(account.GetMuitToBaseInterestRate().ToString());
    }

    private static async Task GetCreditScore(HttpContext ctx, BrokerContext dbctx, long entityid)
    {
        BrokerAccount? account = DBCache.Get<BrokerAccount>(entityid);
        if (account is null) account = await CreateAccount(dbctx, entityid);
        await account.UpdateCreditScore(dbctx);

        await ctx.Response.WriteAsync(account.CreditScore.ToString());
    }

    private static async Task GetMaxLoan(HttpContext ctx, BrokerContext dbctx, long entityid)
    {
        BrokerAccount? account = DBCache.Get<BrokerAccount>(entityid);
        if (account is null) account = await CreateAccount(dbctx, entityid);
        await account.UpdateCreditScore(dbctx);

        await ctx.Response.WriteAsync(account.MaxLoan.ToString());
    }

    private static async Task GetTotalDebt(HttpContext ctx, BrokerContext dbctx, long entityid)
    {
        BrokerAccount? account = DBCache.Get<BrokerAccount>(entityid);
        if (account is null) account = await CreateAccount(dbctx, entityid);

        await ctx.Response.WriteAsync((await dbctx.Loans.Where(x => x.IsActive).SumAsync(x => x.TotalAmount-x.PaidBack)).ToString());
    }
}
