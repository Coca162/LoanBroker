using System.ComponentModel.DataAnnotations;

namespace LoanBroker.Models;
public class BrokerAccount
{
    [Key]
    public string SVID { get; set; }
    public decimal MaxLoan { get; set; }

    // exmaple:
    // Jack has a 10,000 credit loan that is due in 30 days
    // if this is true, then he will pay ~330 a day or 13.8 per hour (4.6 per UBI payment period)
    public bool UseAutoUBIToPayBackLoans { get; set; }
}