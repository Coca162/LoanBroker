using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LoanBroker.Models;

public enum DepositType
{
    Manual = 0,
    UBI = 1,
    FromAutoRepaymentSetting = 2
}

[Index(nameof(IsActive))]
[Index(nameof(Interest))]
public class Deposit
{
    [Key]
    public long Id { get; set; }
    public long AccountId { get; set; }

    [DecimalType(10)]
    public decimal Interest { get; set; }

    [DecimalType(10)]
    public decimal Amount { get; set; }

    [DecimalType(10)]
    public decimal TotalAmount { get; set; }
    public bool IsActive { get; set; }
    public DateTime TimeCreated { get; set; }
    public bool TrackBaselineInterestRate { get; set; }
    public DepositType Type { get; set; }

    [NotMapped]
    [JsonIgnore]
    public BrokerAccount Account => DBCache.Get<BrokerAccount>(AccountId);
}