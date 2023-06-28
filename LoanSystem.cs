using System.Reflection;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Timers;
using LoanBroker.Models;

public static class LoanSystem
{
    public static decimal CurrentBaseInterestRate = 5.00m;
    public static async Task<Deposit> GetCheapestDeposit()
    {
        BrokerContext db = new();
        return await db.Deposits.Where(x => x.IsActive).OrderBy(x => x.Interest)
                                     .SingleAsync();
    }
    public static async Task<Deposit> GetCheapestDeposit(List<long> did)
    {
        BrokerContext db = new();
        return await db.Deposits.Where(x => x.IsActive && !did.Contains(x.Id))
                                    .OrderBy(x => x.Interest)
                                    .SingleAsync();
    }
}
