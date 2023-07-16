global using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using LoanBroker.API;
using LoanBroker;
using LoanBroker.Workers;

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

builder.WebHost.ConfigureKestrel((context, options) =>
{
    //options.Configure(builder.Configuration.GetSection("Kestrel"));
#if DEBUG
    options.Listen(IPAddress.Any, 5001, listenOptions => {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2AndHttp3;
        listenOptions.UseHttps();
    });
#else
    options.Listen(IPAddress.Any, 5001, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2AndHttp3;
        listenOptions.UseHttps();
    });
#endif
});

BrokerContext.DbFactory = BrokerContext.GetDbFactory();

using var dbctx = BrokerContext.DbFactory.CreateDbContext();

string sql = BrokerContext.GenerateSQL();

try
{
    await File.WriteAllTextAsync("../Definitions.sql", sql);
}
catch (Exception e)
{

}

BrokerContext.RawSqlQuery<string>(sql, null, true);

await DBCache.LoadAsync();

LoanSystem.CurrentBaseInterestRate = (await dbctx.Loans.Where(x => x.IsActive).OrderByDescending(x => x.Start).FirstOrDefaultAsync())?.BaseInterest ?? 0.05m;

builder.Services.AddDbContextPool<BrokerContext>(options =>
{
    options.UseNpgsql(BrokerContext.ConnectionString, options => options.EnableRetryOnFailure());
});

builder.Services.AddHostedService<DepositWorker>();

var app = builder.Build();

app.UseRouting();

app.UseCors();

MainAPI.AddRoutes(app);

app.Run();