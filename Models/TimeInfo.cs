using System.ComponentModel.DataAnnotations;

namespace LoanBroker.Models;

public class TimeInfo
{
    [Key]
    public long Id { get; set; }
    public DateTime LastLoanUpdate { get; set; }
}
