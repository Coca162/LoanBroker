using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LoanBroker.Models;

[Index(nameof(AccountId))]
public class CreditScoreHistory
{
    [Key]
    public long Id { get; set; }
    public long AccountId { get; set; }
    public int CreditScore { get; set; }
    public DateTime Date { get; set; }
}
