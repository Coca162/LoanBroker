using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanBroker.Models;

[Index(nameof(AccountId))]
[Index(nameof(IsActive))]
public class Loan
{
    [Key]
    public long Id { get; set; }
    public long AccountId { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidBack { get; set; }
    public decimal Interest { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsActive { get; set; }
    public int TimesLate { get; set; }
    public decimal LateFees { get; set; }
    public decimal LateFeesPaid { get; set; }

    [ForeignKey("AccountId")]
    public BrokerAccount Account { get; set; }

    [InverseProperty("Loan")]
    public List<Loaner> Loaners {  get; set; }
}