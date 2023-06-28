using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanBroker.Models;

[Index(nameof(IsActive))]
[Index(nameof(Interest))]
public class Deposit
{
    [Key]
    public long Id { get; set; }
    public long AccountId { get; set; }
    public decimal Interest { get; set; }
    public decimal Amount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsActive { get; set; }
    public DateTime TimeCreated { get; set; }

    [ForeignKey("AccountId")]
    public BrokerAccount Account { get; set; }
}