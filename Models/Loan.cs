using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LoanBroker.Models;

[Index(nameof(AccountId))]
[Index(nameof(IsActive))]
public class Loan
{
    [Key]
    public long Id { get; set; }
    public long AccountId { get; set; }

    [DecimalType(6)]
    public decimal BaseAmount { get; set; }

    [DecimalType(6)]
    public decimal TotalAmount { get; set; }

    [DecimalType(6)]
    public decimal PaidBack { get; set; }

    [DecimalType(6)]
    public decimal Interest { get; set; }

    [DecimalType(6)]
    public decimal BaseInterest { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public DateTime? TimeFullyPaidBack { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastTimePaid { get; set; }
    public DateTime LastTimeLateFeeWasApplied { get; set; }
    public int TimesLate { get; set; }

    [DecimalType(6)]
    public decimal TotalInterestRate { get; set; }

    [DecimalType(6)]
    public decimal LateFees { get; set; }

    [DecimalType(6)]
    public decimal LateFeesPaid { get; set; }

    [NotMapped]
    public int LengthInMonths => (int)(Start.Subtract(End).TotalDays / 30);

    [NotMapped]
    [JsonIgnore]
    public BrokerAccount Account => DBCache.Get<BrokerAccount>(AccountId);

    [InverseProperty("Loan")]
    public List<Loaner> Loaners {  get; set; }
}