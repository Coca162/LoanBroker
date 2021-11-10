using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LoanBroker.Models;
public class Deposit
{
    [Key]
    public string SVID { get; set; }
    public decimal Interest { get; set; }
    public decimal Amount { get; set; }

    public async Task<BrokerAccount> GetAccount(BrokerWebContext db)
    {
        return await db.BrokerAccounts.FirstOrDefaultAsync(x => x.SVID == SVID);
    }
}