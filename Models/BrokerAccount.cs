using Microsoft.EntityFrameworkCore;
using SpookVooper.Api;
using System.ComponentModel.DataAnnotations;
using static Discord.Commands.UBI;
using static SpookVooper.Api.SpookVooperAPI;

namespace LoanBroker.Models;
public class BrokerAccount
{
    [Key]
    public string SVID { get; set; }
    public ulong DiscordId { get; set; }
    public decimal MaxLoan { get; set; }
    public bool Approved { get; set; }

    // exmaple:
    // Jack has a 10,000 credit loan that is due in 30 days
    // if this is true, then he will pay ~330 a day or 13.8 per hour (4.6 per UBI payment period)
    public bool UseAutoUBIToPayBackLoans { get; set; }

    public async Task UpdateMaxLoan()
    {
        if (!Approved)
        {
            JacobUBIXPData data = await GetDataFromJson<JacobUBIXPData>($"https://ubi.vtech.cf/get_xp_info?id={DiscordId}");
            MaxLoan = data.DailyUBI * 30;
        }
        else
        {
            MaxLoan = 500000;
        }
    }

    public async Task<decimal> GetLentOut(BrokerWebContext db)
    {
        decimal total = 0;
        foreach (Loaner loaner in await db.Loaners.Where(x => x.SVID == SVID).ToListAsync())
        {
            Loan loan = await db.Loans.FirstOrDefaultAsync(x => x.ID == loaner.LoanId);
            total += loaner.Percent * (loan.Amount - loan.PaidBack);
        }
        return total;
    }
    public async Task<decimal> GetAmountDeposited(BrokerWebContext db)
    {
        Deposit deposit = await db.Deposits.FirstOrDefaultAsync(x => x.SVID == SVID);
        if (deposit is null)
        {
            return 0;
        }
        return deposit.Amount;
    }

    public async Task<decimal> GetDepositLeft(BrokerWebContext db)
    {
        return (await GetAmountDeposited(db)) - (await GetLentOut(db));
    }

    public async Task<decimal> GetLoanedAmount(BrokerWebContext db)
    {
        decimal total = 0;
        foreach (Loan loan in await db.Loans.Where(x => x.SVID == SVID).ToListAsync())
        {
            total += loan.Amount - loan.PaidBack;
        }
        return total;
    }

    public async Task<TaskResult> TakeOutLoan(BrokerWebContext db, decimal amount)
    {
        if ((await GetLoanedAmount(db) + amount) > MaxLoan)
        {
            return new TaskResult(false, "You lack the availble credit to take out this loan!");
        }

        decimal got = 0;

        Loan loan = new();
        loan.SVID = SVID;
        loan.Start = DateTime.UtcNow;

        List<List<decimal>> data = new();

        List<Deposit> areadydid = new();

        Deposit cheapest = null;

        while (got < amount)
        {
            while (cheapest is null || await ((await cheapest.GetAccount(db)).GetDepositLeft(db)) <= 0)
            {
                cheapest = await LoanSystem.GetCheapestDeposit(areadydid);
            }

            Loaner loaner = new();
            loaner.SVID = cheapest.SVID;
            loaner.LoanId = loan.ID;

            loan.Loaners.Add(loaner);

            decimal increase = await (await cheapest.GetAccount(db)).GetDepositLeft(db);

            got += increase;

            data.Add(new List<decimal> {increase, cheapest.Interest });

        }
        decimal total = data.Sum(x => x[0]);
        decimal totalrate = data.Sum(x => x[1]*x[0]);

        loan.Interest = totalrate / total;

        loan.Amount = amount*loan.Interest;

        int i = 0;
        foreach (Loaner loaner in loan.Loaners)
        {
            loaner.Percent = data[i][0]/total;
            i += 1;
        }

        return new TaskResult(true, $"Successfully took out a loan of ¢{amount} at avg interest of {Math.Round(loan.Interest * 100, 2)}");
    }
}