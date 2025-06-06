using System;
namespace NewAppErp.Models.Salary;
public class MonthlySalaryChartData
{
    public string Month { get; set; } // Exemple : "2024-03"
    public decimal NetPay { get; set; }
    public Dictionary<string, decimal> Components { get; set; } = new();
}
