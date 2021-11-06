using Microsoft.EntityFrameworkCore;
using LoanBroker.Models;
using Shared.Models;

public class BrokerWebContext : DbContext
{
    public DbSet<Deposit> Deposits { get; set; }
    public DbSet<Loan> Loans { get; set; }
    public DbSet<Loaner> Loaners { get; set; }
    public DbSet<BrokerAccount> BrokerAccounts { get; set; }

    public DbSet<User> Users { get; set; }
    public DbSet<Register> Registers { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    public readonly static string ConnectionString = $@"server={Shared.Main.config.Server};userid={Shared.Main.config.UserID};password={Shared.Main.config.Password};database={Shared.Main.config.Database}";
    public readonly static MySqlServerVersion version = new("8.0.26");

    public BrokerWebContext(DbContextOptions<BrokerWebContext> options) : base(options)
    {
    }
}

//public class BrokerContext : DbContext
//{
//    public DbSet<Deposit> Deposits { get; set; }
//    public DbSet<Loan> Loans { get; set; }
//    public DbSet<Loaner> Loaners { get; set; }
//    public DbSet<BrokerAccount> BrokerAccounts { get; set; }

//    public readonly static string ConnectionString = $@"i am not leaking myself";
//    public readonly static MySqlServerVersion version = new("8.0.26");

//    //public BrokerContext(DbContextOptions<CocaBotWebContext> options) : base(options)
//    //{

//    //}

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
//    optionsBuilder.UseMySql(ConnectionString, version, options => options.EnableRetryOnFailure());

//    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseMySql(ConnectionString, version, options => options.EnableRetryOnFailure());
//}