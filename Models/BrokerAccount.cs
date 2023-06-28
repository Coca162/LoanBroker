using LoanBroker.Managers;
using LoanBroker.Models.NonDBO;
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

public class BrokerAccount
{
    private static readonly HttpClient client = new HttpClient();

    [Key]
    public long Id { get; set; }
    public decimal MaxLoan { get; set; }
    public string Access_Token { get; set; }
    public bool TrustedAccount { get; set; }
    public RepaymentSettingTypes RepaymentSetting { get; set; }

    public async Task UpdateMaxLoan()
    {
        if (!TrustedAccount)
        {
            MaxLoan = 50_000;
        }
        else
        {
            MaxLoan = 500000;
        }
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
            LateFeesPaid = 0.00m
        };

        List<LoanDepositData> data = new();

        List<Deposit> areadydid = new();
        List<long> areadyDidIds = new();

        Deposit cheapest = null;

        while (got < amount)
        {
            while (cheapest is null || await cheapest.Account.GetDepositLeft(dbctx) <= 0)
            {
                cheapest = await LoanSystem.GetCheapestDeposit(areadyDidIds);
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

        loan.TotalAmount = amount * loan.Interest;

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