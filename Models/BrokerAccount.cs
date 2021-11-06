using System.ComponentModel.DataAnnotations;

namespace LoanBroker.Models;
public class BrokerAccount
{
    [Key]
    public string SVID { get; set; }
    public decimal MaxLoan { get; set; }
}