global using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "ApiPolicy",
        policy =>
        {
            policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetIsOriginAllowed(_ => true)
                .AllowAnyOrigin();
        });
});

var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

//builder.Configuration.GetSection("Valour").Get<ValourConfig>();
builder.Configuration.GetSection("Database").Get<DBConfig>();
builder.Configuration.GetSection("SV").Get<SVConfig>();

using var dbctx = BrokerContext.DbFactory.CreateDbContext();
LoanSystem.CurrentBaseInterestRate = (await dbctx.Deposits.Where(x => x.IsActive).OrderByDescending(x => x.Interest).LastOrDefaultAsync())?.Interest ?? 5.00m;

await Task.Delay(-1);