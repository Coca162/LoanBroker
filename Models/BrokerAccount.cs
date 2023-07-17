using LoanBroker.Managers;
using LoanBroker.Models.NonDBO;
using LoanBroker.Models.SVModels;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoanBroker.Models;

public enum RepaymentSettingTypes
{
    None = 0,
    MaintainBalance = 1,
    All = 2
}

public enum EntityType
{
    User = 0,
    Group = 1,
    Corporation = 2,
    Nation = 3
}

public class BrokerAccount
{
    private static readonly HttpClient client = new HttpClient();

    [Key]
    public long Id { get; set; }
    public decimal MaxLoan { get; set; }
    public string Access_Token { get; set; }
    public bool TrustedAccount { get; set; }
    public int CreditScore { get; set; }
    public DateTime? FirstLoan { get; set; } 
    public RepaymentSettingTypes RepaymentSetting { get; set; }

    public decimal GetMuitToBaseInterestRate()
    {
        return (decimal)Math.Min(10, Math.Log(Math.Pow(CreditScore, -1), 1.4) + 21.53);
    }

    [NotMapped]
    [JsonIgnore]
#if DEBUG
    public static string baseurl = "https://localhost:7186";
#else
    public static string baseurl = "https://wug.superjacobl.com";
#endif

    public async Task<List<EntityBalanceRecord>?> GetLast30DaysOfBalanceRecordsAsync()
    {
        var url = $"{baseurl}/api/entities/{Id}/taxablebalancehistory?days=30";
        string stringresult = await client.GetStringAsync(url);
        if (stringresult.Contains("<!DOCTYPE html>"))
            return null;
        return JsonSerializer.Deserialize<List<EntityBalanceRecord>>(await client.GetStringAsync(url));
    }

    public async Task<BaseEntity?> GetEntityAsync()
    {
        var url = $"{baseurl}/api/entities/{Id}";
        string stringresult = await client.GetStringAsync(url);
        if (stringresult.Contains("<!DOCTYPE html>"))
            return null;
        return JsonSerializer.Deserialize<BaseEntity>(await client.GetStringAsync(url));
    }

    public async Task<Group?> GetGroupAsync()
    {
        var url = $"{baseurl}/api/entities/{Id}";
        string stringresult = await client.GetStringAsync(url);
        if (stringresult.Contains("<!DOCTYPE html>"))
            return null;
        return JsonSerializer.Deserialize<Group>(await client.GetStringAsync(url));
    }

    public async Task<List<BaseEntity>> GetOwnershipChain()
    {
        var url = $"{baseurl}/api/groups/{Id}/ownershipchain";
        string stringresult = await client.GetStringAsync(url);
        if (stringresult.Contains("<!DOCTYPE html>"))
            return null;
        return JsonSerializer.Deserialize<List<BaseEntity>>(await client.GetStringAsync(url));
    }

    public static async Task<List<Group>> GetOwnedGroups(long entityId)
    {
        var url = $"{baseurl}/api/entities/{entityId}/ownedgroups";
        string stringresult = await client.GetStringAsync(url);
        if (stringresult.Contains("<!DOCTYPE html>"))
            return null;
        return JsonSerializer.Deserialize<List<Group>>(await client.GetStringAsync(url));
    }

    public async Task UpdateCreditScore(BrokerContext dbctx)
    {
        // TODO: in future, use the entity's networth in calculation
        // TODO: in future, use the International, National, and State GDP's in their calculation of maxloan, and use the debt-to-GDP ratios for credit scores
        var score = 650;
        var maxloan = 25_000.0m;

        var records = await GetLast30DaysOfBalanceRecordsAsync();
        if (records is null)
            return;
        records.Reverse();

        var now = DateTime.UtcNow;
        var fromdate = DateTime.UtcNow.AddDays(-365);
        score -= (await dbctx.Loans.Where(x => x.TimesLate > 0 && x.Start > fromdate).SumAsync(x => x.TimesLate)) * 10;
        var debtcapacityUsed = await GetDebtCapacityUsed(dbctx);

        bool IsDistrict = false;
        bool IsState = false;
        var entity = await GetEntityAsync();
        Group? group = null;
        if (Id == 100 || entity.EntityType == EntityType.Group || entity.EntityType == EntityType.Corporation)
        {
            group = await GetGroupAsync();
            if (group.OwnerId < 200 && group.GroupType != GroupTypes.Nation && group.GroupType != GroupTypes.State && group.GroupType != GroupTypes.Province) 
                score += 50;
            if (group.Flags.Contains(GroupFlag.AccreditedBank))
                score += 75;
            if (group.Id == 100) {
                score += 350;
                maxloan += 10_000_000.0m;
                var url = $"{baseurl}/api/eco/nation/100/GDP";
                var response = await client.GetAsync(url);
                string stringresult = await response.Content.ReadAsStringAsync();
                if (!stringresult.Contains("<!DOCTYPE html>"))
                {
                    var gdp = decimal.Parse(stringresult);
                    if (gdp <= 10_000.0m) gdp = 10_000.0m;
                    maxloan += gdp * 1.5m * 3;
                    
                    // the higher the debt-to-GDP ratio, the lower the score
                    score -= (int)Math.Max((((debtcapacityUsed / gdp) / 1.5m) * 300.0m) - 100.0m, 0.0m);
                }
            }
            if (group.GroupType == GroupTypes.Nation) {
                score += 50;
                IsDistrict = true;
                var url = $"{baseurl}/api/eco/nation/{Id}/GDP";
                var response = await client.GetAsync(url);
                string stringresult = await response.Content.ReadAsStringAsync();
                if (!stringresult.Contains("<!DOCTYPE html>"))
                {
                    var gdp = decimal.Parse(stringresult);
                    if (gdp <= 10_000.0m) gdp = 10_000.0m;
                    maxloan += gdp * 1.25m;
                    score -= (int)Math.Max((((debtcapacityUsed / gdp) / 0.75m) * 300.0m) - 100.0m, 0.0m);
                }
            }
            else if (group.GroupType == GroupTypes.State) {
                score += 50;
                IsState = true;
                var url = $"{baseurl}/api/eco/state/{Id}/GDP";
                var response = await client.GetAsync(url);
                string stringresult = await response.Content.ReadAsStringAsync();
                if (!stringresult.Contains("<!DOCTYPE html>"))
                {
                    var gdp = decimal.Parse(stringresult);
                    if (gdp <= 10_000.0m) gdp = 10_000.0m;
                    maxloan += gdp * 0.35m;
                    score -= (int)Math.Max((((debtcapacityUsed / gdp) / 0.25m) * 300.0m) - 125.0m, 0.0m);
                }
            }
            else if (group.GroupType == GroupTypes.Province) {

            }
            else {
                if (MaxLoan > 0.0m)
                    score -= (int)Math.Max(((debtcapacityUsed / MaxLoan) * 400.0m) - 150.0m, 0.0m);
            }
        }

        var monthlyprofit = 0.00m;
        
        if (records.Count < 3) {
            records.Add(new() { Balance = 0.0m, TaxableBalance = 0.0m } );
        }
        records.Add(new() { Balance = entity.Balance, TaxableBalance = entity.TaxableBalance});
        var lastbalance = records.First().TaxableBalance;
        foreach (var record in records)
        {
            monthlyprofit += record.TaxableBalance - lastbalance;
            lastbalance = record.TaxableBalance;
        }

        monthlyprofit *= Math.Max(1.0m, 30.0m / records.Count);

        if (monthlyprofit > 0.00m)
        {
            // means entity has made a profit
            var add = monthlyprofit * 6 / 2.5m;
            if (group.GroupType is GroupTypes.Nation or GroupTypes.State or GroupTypes.Province)
                maxloan += add / 10.0m;
            else
                maxloan += add;
            score += (int)Math.Pow((double)monthlyprofit, 0.4);
        }
        else if (monthlyprofit > -1000.00m)
        {
            // means entity has made a small loss, most likey a newer entity
        }
        else
        {
            // means entity has made a loss and their credit score should be reduced because of that
        }

        CreditScore = score;
        if (group is not null && group.OwnerId < 200 && group.GroupType != GroupTypes.Nation && group.GroupType != GroupTypes.State && group.GroupType != GroupTypes.Province) 
            maxloan *= 1.5m;
        else if (group is null || group.GroupType is GroupTypes.Company or GroupTypes.Corporation)
            maxloan *= 1.15m;

        if (score >= 999)
            score = 999;

        if (score > 950)
            maxloan += 250_000.0m;
        else if (score > 900)
            maxloan += 200_000.0m;
        else if (score > 800)
            maxloan += 100_000.0m;
        else if (score > 750)
            maxloan += 40_000.0m;
        else if (score > 700)
            maxloan += 25_000.0m;
        if (group is null || (group.GroupType != GroupTypes.Nation && group.GroupType != GroupTypes.State && group.GroupType != GroupTypes.Province)) {
            if (maxloan > 750_000.0m)
            {
                var leftover = maxloan - 750_000.0m;
                maxloan = 750_000.0m + (decimal)Math.Pow((double)leftover, 0.925);
            }
            double hoursSinceFirstLoan = 0;
            if (FirstLoan is not null)
                hoursSinceFirstLoan = DateTime.UtcNow.Subtract((DateTime)FirstLoan).TotalHours;
            if (false) {
                if (maxloan > 30_000.0m) {
                    var leftover = maxloan - 30_000.0m;
                    if (hoursSinceFirstLoan < 24 * 7) {
                        leftover *= leftover / ((decimal)hoursSinceFirstLoan / (24.0m * 7.0m));
                    }
                    maxloan = 30_000.0m + leftover;
                }
            }
        }
        if (Id != 100 && maxloan > 50_000.0m) {
            double hoursSinceFirstLoan = 0;
            if (FirstLoan is not null)
                hoursSinceFirstLoan = DateTime.UtcNow.Subtract((DateTime)FirstLoan).TotalHours;
            hoursSinceFirstLoan += 1.0;
            var leftover = maxloan - 50_000.0m;
            if (hoursSinceFirstLoan < 24 * 7) {
                leftover *= ((decimal)hoursSinceFirstLoan / (24.0m * 7.0m));
            }
            maxloan = 50_000.0m + leftover;
        }
        MaxLoan = maxloan;
    }

    public async Task<decimal> GetLentOut(BrokerContext dbctx)
    {
        decimal total = 0;
        foreach (Loaner loaner in await dbctx.Loaners.Include(x => x.Loan).Where(x => x.LoanerAccountId == Id).ToListAsync())
        {
            total += loaner.Percent * (loaner.Loan.BaseAmount - loaner.Loan.PaidBack);
        }
        return total;
    }
    public async Task<decimal> GetAmountDeposited(BrokerContext dbctx)
    {
        return await dbctx.Deposits.Where(x => x.AccountId == Id).SumAsync(x => x.Amount);
    }

    public async Task<decimal> GetDepositLeft(BrokerContext dbctx)
    {
        return (await GetAmountDeposited(dbctx)) - (await GetLentOut(dbctx));
    }

    public async Task<decimal> GetDebtCapacityUsed(BrokerContext dbctx)
    {
        return (await dbctx.Loans.Where(x => x.AccountId == Id && x.IsActive).ToListAsync()).Sum(x => (1.0m / (1.0m + x.TotalInterestRate)) * (x.TotalAmount - x.PaidBack));
    }

    public async Task<TaskResult> TakeOutLoan(BrokerContext dbctx, decimal amount, long lengthindays)
    {
        if ((await GetDebtCapacityUsed(dbctx) + amount) > MaxLoan)
            return new TaskResult(false, "You lack the availble credit to take out this loan!");

        var ownershipChain = await GetOwnershipChain();
        var lastSeparateEntity = ownershipChain.FirstOrDefault(x => x.EntityType == EntityType.User || (x.EntityType == EntityType.Group && ((Group)x).Flags.Contains(GroupFlag.SeparateEntityFromOwner)));
        var getLastSeparateEntityOwnedGroups = await GetOwnedGroups(lastSeparateEntity.Id);
        var getLastSeparateEntityOwnedGroupsIds = getLastSeparateEntityOwnedGroups.Select(x => x.Id).ToList();

        // grab total debt capacity used by ALL deposit account the Last Seperate Entity
        var totalDCUsedByChildren = (await dbctx.Loans.Where(x => getLastSeparateEntityOwnedGroupsIds.Contains(x.AccountId) && x.IsActive).ToListAsync()).Sum(x => (1.0m / (1.0m + x.TotalInterestRate)) * (x.TotalAmount - x.PaidBack));
        if (totalDCUsedByChildren + amount >= 1_500_000.0m)
            return new TaskResult(false, $"Debt (not including interest) from All groups whose {lastSeparateEntity.Name} is their highest level owner that has the SeparateFromOwner Flag (users automatically have this flag), is ${totalDCUsedByChildren:n0}. Taking out a loan for this account of this amount will make the prev stated amount go over the limit of $1,500,000! Contract Superjacobl, the CFV, to make a request to give this acccount the SeparateFromOwner flag. Basically, if Super Pog Inc owns A Inc and B Inc, and A Inc has $1,000,000 of debt (excluding interest) and B Inc has $200,000 of debt, then A Inc or B Inc can only take out $100,000 more dollars of debt.");

        decimal got = 0;
        decimal leftover = 0.0m;

        Loan loan = new()
        {
            Id = IdManagers.GeneralIdGenerator.Generate(),
            AccountId = Id,
            BaseAmount = amount,
            PaidBack = 0.00m,
            Start = DateTime.UtcNow,
            IsActive = true,
            End = DateTime.UtcNow.AddDays(lengthindays),
            TimesLate = 0,
            LateFees = 0.00m,
            LateFeesPaid = 0.00m,
            LastTimeLateFeeWasApplied = DateTime.UtcNow,
            LastTimePaid = DateTime.UtcNow,
            Loaners = new()
        };

        List<LoanDepositData> data = new();

        List<Deposit> areadydid = new();
        List<long> areadyDidIds = new();

        Deposit cheapest = null;

        List<Deposit> Deposits = await dbctx.Deposits.Where(x => x.IsActive)
                                    .OrderBy(x => x.Interest).ThenBy(x => x.TimeCreated)
                                    .ToListAsync();

        while (got < amount)
        {
            cheapest = Deposits.FirstOrDefault(x => !areadyDidIds.Contains(x.Id));
            if (cheapest is null)
            {
                break;
            }

            Loaner loaner = new()
            {
                Id = IdManagers.GeneralIdGenerator.Generate(),
                LoanId = loan.Id,
                LoanerAccountId = cheapest.AccountId
            };

            loan.Loaners.Add(loaner);

            leftover = amount - got;
            decimal increase = cheapest.Amount;
            if (increase > leftover)
                increase = leftover;

            leftover -= increase;
            got += increase;

            areadyDidIds.Add(loaner.Id);

            data.Add(new() {
                AmountUsed = increase, 
                InterestRate = cheapest.Interest,
                Deposit = cheapest
            });

        }

        decimal total = data.Sum(x => x.AmountUsed);
        decimal totalrate = data.Sum(x => x.InterestRate * x.AmountUsed);

        loan.Interest = totalrate / total;
        loan.BaseInterest = loan.Interest;
        loan.Interest *= GetMuitToBaseInterestRate();

        loan.TotalAmount = amount;
        var totatInterestRate = loan.Interest / 30.0m * (decimal)(loan.End.Subtract(loan.Start).TotalDays);
        loan.TotalAmount += amount * totatInterestRate;

        int i = 0;
        foreach (Loaner loaner in loan.Loaners)
        {
            loaner.Percent = data[i].AmountUsed / total;
            i += 1;
        }

        SVTransaction tran = new(AccountSystem.GroupSVID, Id, amount, SVConfig.instance.GroupApiKey, "Loan from NVTech Loan Broker", 1);
        TaskResult result = await tran.ExecuteAsync(client);
        if (result.Succeeded)
        {
            LoanSystem.CurrentBaseInterestRate = loan.BaseInterest;
            foreach (var dataobject in data)
            {
                dataobject.Deposit.Amount -= dataobject.AmountUsed;
                if (dataobject.Deposit.Amount <= 0.01m)
                    dataobject.Deposit.IsActive = false;
            }
            loan.TotalInterestRate = loan.TotalAmount / loan.BaseAmount - 1.0m;
            dbctx.Loans.Add(loan);
            dbctx.Loaners.AddRange(loan.Loaners);
            await dbctx.SaveChangesAsync();
        }
        else
        {
            return new TaskResult(false, $"Error: {result.Message}");
        }

        return new TaskResult(true, $"Successfully took out a loan of ¢{amount} at avg interest of {Math.Round(loan.Interest * 100, 2)}");
    }
}

public class LoanDepositData
{
    public decimal AmountUsed { get; set; }
    public decimal InterestRate { get; set; }
    public Deposit Deposit { get; set; }
}