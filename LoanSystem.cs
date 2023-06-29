using System.Reflection;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Timers;
using LoanBroker.Models;

public static class LoanSystem
{
    public static decimal CurrentBaseInterestRate = 0.05m;
    public static async Task<Deposit> GetCheapestDeposit(BrokerContext dbctx, List<long> did)
    {
        return await dbctx.Deposits.Where(x => x.IsActive && !did.Contains(x.Id))
                                    .OrderBy(x => x.Interest).ThenBy(x => x.TimeCreated)
                                    .SingleAsync();
    }
}
