using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanBroker.Models;

public class LoanRequest
{
    public long AccountId { get; set; }
    public decimal Amount { get; set; }
    public long LengthInDays { get; set; }
}