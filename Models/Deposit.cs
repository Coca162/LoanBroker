using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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
    public bool TrackBaselineInterestRate { get; set; }

    [NotMapped]
    [JsonIgnore]
    public BrokerAccount Account => DBCache.Get<BrokerAccount>(AccountId);
}