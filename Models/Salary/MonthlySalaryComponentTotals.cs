using System;
namespace NewAppErp.Models.Salary;

public class MonthlySalaryComponentTotals
{
    public string Month { get; set; } = default!;
    public Dictionary<string, decimal> Components { get; set; } = new();
    public decimal NetPay { get; set; }

    public decimal GrossPay { get; set; }

    public decimal TotalDeduction { get; set; }
}