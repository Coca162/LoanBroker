global using NpgsqlTypes;
using Microsoft.EntityFrameworkCore;
using LoanBroker.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;
using System.Text;
using System.Data.Common;
using System.Data;

/// <summary>A replacement for <see cref="NpgsqlSqlGenerationHelper"/>
/// to convert PascalCaseCsharpyIdentifiers to alllowercasenames.
/// So table and column names with no embedded punctuation
/// get generated with no quotes or delimiters.</summary>
public class NpgsqlSqlGenerationLowercasingHelper : NpgsqlSqlGenerationHelper
{
    //Don't lowercase ef's migration table
    const string dontAlter = "__EFMigrationsHistory";
    static string Customize(string input) => input == dontAlter ? input : input.ToLower();
    public NpgsqlSqlGenerationLowercasingHelper(RelationalSqlGenerationHelperDependencies dependencies)
        : base(dependencies) { }
    public override string DelimitIdentifier(string identifier)
        => base.DelimitIdentifier(Customize(identifier));
    public override void DelimitIdentifier(StringBuilder builder, string identifier)
        => base.DelimitIdentifier(builder, Customize(identifier));
}

public class BrokerContext : DbContext
{
    public static ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));
    public static PooledDbContextFactory<BrokerContext> DbFactory;
    public static string ConnectionString = $"Host={DBConfig.instance.Host};Database={DBConfig.instance.Database};Username={DBConfig.instance.Username};Pwd={DBConfig.instance.Password};Include Error Detail=true";

    public static PooledDbContextFactory<BrokerContext> GetDbFactory()
    {
        var options = new DbContextOptionsBuilder<BrokerContext>()
            .UseNpgsql(ConnectionString, options =>
            {
                options.EnableRetryOnFailure();
            })
            .ReplaceService<ISqlGenerationHelper, NpgsqlSqlGenerationLowercasingHelper>()
            //.UseLoggerFactory(loggerFactory)
            //.LogTo(Console.WriteLine)
            //.EnableSensitiveDataLogging()
            .Options;
        return new PooledDbContextFactory<BrokerContext>(options);
    }

    public BrokerContext(DbContextOptions options)
    {

    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(ConnectionString, options =>
        {
            options.EnableRetryOnFailure();
        });
        //options.UseLoggerFactory(loggerFactory);  //tie-up DbContext with LoggerFactory object
        options.ReplaceService<ISqlGenerationHelper, NpgsqlSqlGenerationLowercasingHelper>();
        //options.UseLoggerFactory(loggerFactory);
        //options.LogTo(Console.WriteLine);
        //options.EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    public static string GenerateSQL()
    {
        using var dbctx = DbFactory.CreateDbContext();
        string sql = dbctx.Database.GenerateCreateScript();
        sql = sql.Replace("numeric(20,0) ", "BIGINT ");
        sql = sql.Replace("CREATE TABLE", "CREATE TABLE IF NOT EXISTS");
        sql = sql.Replace("CREATE INDEX", "CREATE INDEX IF NOT EXISTS");
        return sql;
    }

    public static List<T> RawSqlQuery<T>(string query, Func<DbDataReader, T>? map, bool noresult = false)
    {
        using var dbctx = DbFactory.CreateDbContext();
        using DbCommand command = dbctx.Database.GetDbConnection().CreateCommand();
        command.CommandText = query;
        command.CommandType = CommandType.Text;

        //Console.WriteLine(ConfigManger.Config);

        dbctx.Database.OpenConnection();

        using var result = command.ExecuteReader();
        if (!noresult)
        {
            var entities = new List<T>();

            while (result.Read())
            {
                entities.Add(map(result));
            }

            return entities;
        }
        return new List<T>();
    }

    /// <summary>
    /// This is only here to fulfill the need of the constructor.
    /// It does literally nothing at all.
    /// </summary>
    public static DbContextOptions DBOptions;

    public DbSet<Deposit> Deposits { get; set; }
    public DbSet<Loan> Loans { get; set; }
    public DbSet<Loaner> Loaners { get; set; }
    public DbSet<BrokerAccount> BrokerAccounts { get; set; }
    public DbSet<TimeInfo> TimeInfos { get; set; }
}