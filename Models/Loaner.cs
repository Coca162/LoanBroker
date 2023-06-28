using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanBroker.Models;
public class Loaner
{
    [Key]
    public long Id { get; set; }
    public long LoanId { get; set; }
    public long LoanerAccountId { get; set; }
    public decimal Percent { get; set; }

    [ForeignKey(nameof(LoanerAccountId))]
    public BrokerAccount LoanerAccount { get; set; }

    [ForeignKey(nameof(LoanId))]
    public Loan Loan { get; set; }
}