using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LoanBroker.Models;
public class Loaner
{
    [Key]
    public long Id { get; set; }
    public long LoanId { get; set; }
    public long LoanerAccountId { get; set; }

    [DecimalType(8)]
    public decimal Percent { get; set; }

    [NotMapped]
    [JsonIgnore]
    public BrokerAccount LoanerAccount => DBCache.Get<BrokerAccount>(LoanerAccountId);

    [ForeignKey(nameof(LoanId))]
    [JsonIgnore]
    public Loan Loan { get; set; }
}