using System.Reflection;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Timers;
using LoanBroker.Models;
using System.Security.Principal;
using Valour.Shared;
using IdGen;
using LoanBroker.Models.NonDBO;
using LoanBroker.Managers;

public static class AccountSystem
{
    private static readonly HttpClient client = new HttpClient();
    // the svid of the deposit group
#if DEBUG
    public static long GroupSVID = 20332568570233088;
#else
    public static long GroupSVID = 0;
#endif

    public static async Task DoHourlyTick()
    {
        using var dbctx = BrokerContext.DbFactory.CreateDbContext();
        List<Loan> loans = await dbctx.Loans.Include(x => x.Account).Include(x => x.Loaners).ThenInclude(x => x.LoanerAccount).ToListAsync();

        Dictionary<long, decimal> AmountToReceive = new();
        foreach (Loan loan in loans)
        {
            TimeSpan left = loan.End - DateTime.Now;

            decimal periods = (decimal)((loan.End-DateTime.UtcNow).TotalHours);

            decimal rate = (loan.TotalAmount - loan.PaidBack) / periods;
            decimal latefeesrate = (loan.LateFees - loan.LateFeesPaid) / 72.00m;
            decimal topay = latefeesrate + rate;

            SVTransaction tran = new(loan.AccountId, AccountSystem.GroupSVID, topay, loan.Account.Access_Token, "Loan repayment to NVTech Loan Broker", 9);
            TaskResult result = await tran.ExecuteAsync(client);

            if (result.Success)
            {
                loan.PaidBack += rate;
                loan.LateFeesPaid += latefeesrate;
                foreach (var loaner in loan.Loaners)
                {
                    var amount = loaner.Percent * topay;
                    if (loaner.LoanerAccount.RepaymentSetting == RepaymentSettingTypes.All)
                    {
                        dbctx.Deposits.Add(new()
                        {
                            Id = IdManagers.GeneralIdGenerator.Generate(),
                            AccountId = loaner.LoanerAccountId,
                            Interest = LoanSystem.CurrentBaseInterestRate,
                            Amount = amount
                        });
                    }
                }
            }
            else
            {

            }

        }
        await dbctx.SaveChangesAsync();
    }


}
