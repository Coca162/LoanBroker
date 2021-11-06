using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LoanBroker.Models;
public class Loan
{
    [Key]
    public int ID { get; set; }
    public string SVID { get; set; }
    public decimal Amount { get; set; }
    public decimal Interest { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public ICollection<Loaner> Loaners {  get; set; }
}