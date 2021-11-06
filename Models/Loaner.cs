using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanBroker.Models;
public class Loaner
{
    [Key]
    public int ID { get; set; }
    public string SVID { get; set; }
    public decimal Percent { get; set; }
}