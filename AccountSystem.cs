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
using Microsoft.Extensions.Logging;

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

        Dictionary<long, decimal> AmountToReceiveToSV = new();
        Dictionary<long, decimal> AmountToReceiveToReDeposit = new();
        foreach (Loan loan in loans)
        {
            if (loan.PaidBack + 0.1m >= loan.TotalAmount || loan.End.Subtract(DateTime.UtcNow).TotalHours < 0)
            {
                loan.IsActive = false;
                loan.TimeFullyPaidBack = DateTime.UtcNow;
            }

            decimal periods = (decimal)(Math.Max(1, (loan.End-DateTime.UtcNow).TotalHours));

            decimal rate = (loan.TotalAmount - loan.PaidBack) / periods;
            decimal latefeesrate = (loan.LateFees - loan.LateFeesPaid) / periods;
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
                        if (!AmountToReceiveToReDeposit.ContainsKey(loaner.LoanerAccountId))
                            AmountToReceiveToReDeposit[loaner.LoanerAccountId] = 0;
                        AmountToReceiveToReDeposit[loaner.LoanerAccountId] += amount;
                    }
                    else if (loaner.LoanerAccount.RepaymentSetting == RepaymentSettingTypes.MaintainBalance)
                    {
                        var amountToDeposit = loaner.Percent * rate * (1 - loan.TotalInterestRate);
                        if (!AmountToReceiveToReDeposit.ContainsKey(loaner.LoanerAccountId))
                            AmountToReceiveToReDeposit[loaner.LoanerAccountId] = 0;
                        AmountToReceiveToReDeposit[loaner.LoanerAccountId] += amount;

                        if (!AmountToReceiveToSV.ContainsKey(loaner.LoanerAccountId))
                            AmountToReceiveToSV[loaner.LoanerAccountId] = 0;
                        AmountToReceiveToSV[loaner.LoanerAccountId] += amount - amountToDeposit;
                    }
                    else
                    {
                        if (!AmountToReceiveToSV.ContainsKey(loaner.LoanerAccountId))
                            AmountToReceiveToSV[loaner.LoanerAccountId] = 0;
                        AmountToReceiveToSV[loaner.LoanerAccountId] += amount;
                    }
                }
            }
            else
            {
                if (result.Message == "SV is down")
                {
                    await Task.Delay(5000);
                    continue;
                }
                else if (result.Message.Contains("cannot afford to send"))
                {
                    if (DateTime.UtcNow.Subtract(loan.LastTimePaid).TotalHours >= 24)
                    {
                        if (DateTime.UtcNow.Subtract(loan.LastTimeLateFeeWasApplied).TotalHours >= 24)
                        {
                            // first time late is 2.5% fee on baseamount
                            // every time from then on out is 0.75%
                            if (loan.TimesLate == 0)
                            {
                                loan.LateFees += loan.BaseAmount * 0.025m;
                            }
                            else
                            {
                                loan.LateFees += loan.BaseAmount * 0.0075m;
                            }
                            loan.TimesLate += 1;
                            loan.LastTimeLateFeeWasApplied = DateTime.UtcNow;
                        }
                    }
                }
            }
        }

        foreach (var pair in AmountToReceiveToReDeposit)
        {
            var deposit = new Deposit()
            {
                Id = IdManagers.GeneralIdGenerator.Generate(),
                AccountId = pair.Key,
                Interest = LoanSystem.CurrentBaseInterestRate,
                Amount = pair.Value,
                TotalAmount = pair.Value,
                IsActive = true,
                TimeCreated = DateTime.UtcNow,
            };
            dbctx.Deposits.Add(deposit);
        }

        foreach (var pair in AmountToReceiveToSV)
        {
            SVTransaction tran = new(AccountSystem.GroupSVID, pair.Key, pair.Value, SVConfig.instance.GroupApiKey, "Loan Repayment from NVTech Loan Broker", 1);
            TaskResult result = await tran.ExecuteAsync(client);
            if (result.Success)
            {

            }
            else
            {
                // in case of failure just add it as a deposit
                var deposit = new Deposit()
                {
                    Id = IdManagers.GeneralIdGenerator.Generate(),
                    AccountId = pair.Key,
                    Interest = LoanSystem.CurrentBaseInterestRate,
                    Amount = pair.Value,
                    TotalAmount = pair.Value,
                    IsActive = true,
                    TimeCreated = DateTime.UtcNow,
                };
                dbctx.Deposits.Add(deposit);
            }
        }

        await dbctx.SaveChangesAsync();
    }


}