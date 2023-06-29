using LoanBroker.Managers;
using LoanBroker.Models.NonDBO;
using LoanBroker.Models.SVModels;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Valour.Shared;

namespace LoanBroker.Models;

public enum RepaymentSettingTypes
{
    None = 0,
    MaintainBalance = 1,
    All = 2
}

public enum EntityType
{
    User,
    Group,
    Corporation,
    District
}

public class BrokerAccount
{
    private static readonly HttpClient client = new HttpClient();

    [Key]
    public long Id { get; set; }
    public decimal MaxLoan { get; set; }
    public string Access_Token { get; set; }
    public bool TrustedAccount { get; set; }
    public int CreditScore { get; set; }
    public RepaymentSettingTypes RepaymentSetting { get; set; }

    public decimal GetMuitToBaseInterestRate()
    {
        return (decimal)Math.Min(10, Math.Log(Math.Pow(CreditScore, -1), 1.4) + 21.53);
    }

    public string GetBaseUrl()
    {
#if DEBUG
        var baseurl = "https://localhost:7186";
#else
        var baseurl = "https://spookvooper.com";
#endif
        return baseurl;
    }

    public async Task<List<EntityBalanceRecord>?> GetLast30DaysOfBalanceRecordsAsync()
    {
        var url = $"{GetBaseUrl()}/api/entities/{Id}/taxablebalancehistory?days=30";
        string stringresult = await client.GetStringAsync(url);
        if (stringresult.Contains("<!DOCTYPE html>"))
            return null;
        return JsonSerializer.Deserialize<List<EntityBalanceRecord>>(await client.GetStringAsync(url));
    }

    public async Task<BaseEntity?> GetEntityAsync()
    {
        var url = $"{GetBaseUrl()}/api/entities/{Id}";
        string stringresult = await client.GetStringAsync(url);
        if (stringresult.Contains("<!DOCTYPE html>"))
            return null;
        return JsonSerializer.Deserialize<BaseEntity>(await client.GetStringAsync(url));
    }

    public async Task UpdateCreditScore(BrokerContext dbctx)
    {
        // TODO: in future, use the entity's networth in calculation
        var score = 650;
        var maxloan = 25_000.0m;

        var records = await GetLast30DaysOfBalanceRecordsAsync();
        if (records is null)
            return;
        records.Reverse();

        var now = DateTime.UtcNow;
        var fromdate = DateTime.UtcNow.AddDays(-365);
        var loans = await dbctx.Loans.Where(x => x.TimesLate > 0 && x.Start > fromdate).ToListAsync();
        score -= loans.Sum(x => x.TimesLate) * 10;

        bool IsDistrict = false;
        bool IsState = false;
        var entity = await GetEntityAsync();
        if (entity.EntityType == EntityType.Group || entity.EntityType == EntityType.Corporation)
        {
            Group group = (Group)entity;
            if (group.Flags.Contains(GroupFlag.AccreditedBank))
                score += 75;
            if (group.GroupType == GroupTypes.District) {
                score += 50;
                IsDistrict = true;
            }
            else if (group.GroupType == GroupTypes.State) {
                score += 50;
                IsState = true;
            }
        }

        if (records.Count >= 3)
        {
            var monthlyprofit = 0.00m;
            var lastbalance = records.First().TaxableBalance;
            foreach (var record in records)
            {
                monthlyprofit += record.TaxableBalance - lastbalance;
                lastbalance = record.TaxableBalance;
            }

            monthlyprofit *= Math.Max(1.0m, 30.0m / records.Count);

            if (monthlyprofit > 0.00m)
            {
                // means entity has made a profit
                maxloan += monthlyprofit * 6 / 2.5m;
                score += (int)Math.Pow((double)monthlyprofit, 0.4);
            }
            else if (monthlyprofit > -1000.00m)
            {
                // means entity has made a small loss, most likey a newer entity
            }
            else
            {
                // means entity has made a loss and their credit score should be reduced because of that
            }
        } 

        CreditScore = score;

        if (score > 950)
            maxloan += 250_000.0m;
        else if (score > 900)
            maxloan += 200_000.0m;
        else if (score > 800)
            maxloan += 100_000.0m;
        else if (score > 750)
            maxloan += 40_000.0m;
        else if (score > 700)
            maxloan += 25_000.0m;
        MaxLoan = maxloan;
    }

    public async Task<decimal> GetLentOut(BrokerContext dbctx)
    {
        decimal total = 0;
        foreach (Loaner loaner in await dbctx.Loaners.Include(x => x.Loan).Where(x => x.LoanerAccountId == Id).ToListAsync())
        {
            total += loaner.Percent * (loaner.Loan.BaseAmount - loaner.Loan.PaidBack);
        }
        return total;
    }
    public async Task<decimal> GetAmountDeposited(BrokerContext dbctx)
    {
        return await dbctx.Deposits.Where(x => x.AccountId == Id).SumAsync(x => x.Amount);
    }

    public async Task<decimal> GetDepositLeft(BrokerContext dbctx)
    {
        return (await GetAmountDeposited(dbctx)) - (await GetLentOut(dbctx));
    }

    public async Task<decimal> GetLoanedAmount(BrokerContext dbctx)
    {
        return await dbctx.Loans.Where(x => x.AccountId == Id).SumAsync(x => x.TotalAmount - x.PaidBack);
    }

    public async Task<TaskResult> TakeOutLoan(BrokerContext dbctx, decimal amount, long lengthindays)
    {
        if ((await GetLoanedAmount(dbctx) + amount) > MaxLoan)
            return new TaskResult(false, "You lack the availble credit to take out this loan!");

        decimal got = 0;
        decimal leftover = 0.0m;

        Loan loan = new()
        {
            Id = IdManagers.GeneralIdGenerator.Generate(),
            AccountId = Id,
            BaseAmount = amount,
            PaidBack = 0.00m,
            Start = DateTime.UtcNow,
            IsActive = true,
            End = DateTime.UtcNow.AddDays(lengthindays),
            TimesLate = 0,
            LateFees = 0.00m,
            LateFeesPaid = 0.00m,
            LastTimeLateFeeWasApplied = DateTime.UtcNow,
            LastTimePaid = DateTime.UtcNow
        };

        List<LoanDepositData> data = new();

        List<Deposit> areadydid = new();
        List<long> areadyDidIds = new();

        Deposit cheapest = null;

        while (got < amount)
        {
            while (cheapest is null || await cheapest.Account.GetDepositLeft(dbctx) <= 0)
            {
                cheapest = await LoanSystem.GetCheapestDeposit(dbctx, areadyDidIds);
            }

            Loaner loaner = new()
            {
                Id = IdManagers.GeneralIdGenerator.Generate(),
                LoanId = loan.Id,
                LoanerAccountId = cheapest.AccountId
            };

            loan.Loaners.Add(loaner);

            decimal increase = cheapest.Amount;
            if (increase > leftover)
                increase = leftover;

            leftover -= increase;
            got += increase;

            data.Add(new() {
                AmountUsed = increase, 
                InterestRate = cheapest.Interest,
                Deposit = cheapest
            });

        }

        decimal total = data.Sum(x => x.AmountUsed);
        decimal totalrate = data.Sum(x => x.InterestRate * x.AmountUsed);

        loan.Interest = totalrate / total;
        loan.Interest *= GetMuitToBaseInterestRate();

        loan.TotalAmount = amount;
        var totatInterestRate = loan.Interest / 30.0m * (decimal)(loan.End.Subtract(loan.Start).TotalDays);
        loan.TotalAmount += amount * totatInterestRate;

        int i = 0;
        foreach (Loaner loaner in loan.Loaners)
        {
            loaner.Percent = data[i].AmountUsed / total;
            i += 1;
        }

        SVTransaction tran = new(AccountSystem.GroupSVID, Id, amount, SVConfig.instance.GroupApiKey, "Loan from NVTech Loan Broker", 1);
        TaskResult result = await tran.ExecuteAsync(client);
        if (result.Success)
        {
            LoanSystem.CurrentBaseInterestRate = loan.Interest;
            foreach (var dataobject in data)
            {
                dataobject.Deposit.Amount -= dataobject.AmountUsed;
                if (dataobject.Deposit.Amount <= 0.01m)
                    dataobject.Deposit.IsActive = false;
            }
        }
        else
        {

        }

        return new TaskResult(true, $"Successfully took out a loan of ¢{amount} at avg interest of {Math.Round(loan.Interest * 100, 2)}");
    }
}

public class LoanDepositData
{
    public decimal AmountUsed { get; set; }
    public decimal InterestRate { get; set; }
    public Deposit Deposit { get; set; }
}