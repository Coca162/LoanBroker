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

public static class LoanSystem
{
    public static async Task<Deposit> GetCheapestDeposit()
    {
        BrokerContext db = new();
        return await db.Deposits.OrderBy(x => x.Interest)
                                     .SingleAsync();
    }
}
