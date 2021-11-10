using System.ComponentModel.DataAnnotations;

namespace LoanBroker.Models;
public class Deposit
{
    [Key]
    public string SVID { get; set; }
    public decimal Interest { get; set; }
    public decimal Amount { get; set; }
}