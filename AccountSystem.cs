using DSharpPlus;
using DSharpPlus.CommandsNext;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Timers;
using SpookVooper.Api;
using SpookVooper.Api.Entities;
using DSharpPlus.Entities;
using static Shared.Main;
using LoanBroker.Models;
using SpookVooper.Api.Economy;

public static class AccountSystem
{
    public static void HookTransactionHub()
    {
        // Create transaction hub object
        TransactionHub tHub = new();

        // Hook transaction event to method
        tHub.OnTransaction += HandleTransaction;
    }

    static async void HandleTransaction(Transaction transaction)
    {

        if (transaction.Detail != "UBI Payment" || transaction.FromAccount != "g-a79c4e06-ca17-4212-8e75-3964e8fe7015")
        {
            return;
        }

        BrokerContext db = new();

        // get account from db

        BrokerAccount account = await db.BrokerAccounts.SingleOrDefaultAsync(x => x.SVID == transaction.FromAccount);

        if (account != null)
        {
            if (account.UseAutoUBIToPayBackLoans)
            {
                List<Loan> loans = await db.Loans.Include(x => x.Loaners).Where(x => x.SVID == account.SVID).ToListAsync();
                
                foreach (Loan loan in loans)
                {

                    TimeSpan left = loan.End - DateTime.Now;

                    // num of UBI pay periods 

                    decimal periods = (decimal)left.TotalMinutes / 20;

                    decimal rate = loan.Amount / periods;

                    foreach(Loaner loaner in loan.Loaners)
                    {

                        CocaBotContext cb = new();

                        string token = (await cb.Users.FindAsync(transaction.FromAccount)).Token;

                        Entity fromEntity = new(transaction.FromAccount);
                        fromEntity.Auth_Key = token + "|" + Main.config.OauthSecret;

                        TaskResult results = await fromEntity.SendCreditsAsync(rate*loaner.Percent, loaner.SVID, $"Auto UBI Loan RePayment");
                    }



                }
            }
        }
    }


}
