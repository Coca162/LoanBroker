using Microsoft.EntityFrameworkCore;
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
            total += loaner.Percent * (await db.Loans.FirstOrDefaultAsync(x => x.ID == loaner.LoanId)).Amount;
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
}