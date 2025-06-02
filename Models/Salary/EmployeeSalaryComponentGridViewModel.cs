using System;
namespace NewAppErp.Models.Salary;

public class EmployeeSalaryComponentGridViewModel
{
    public string EmployeeName { get; set; }
    public string Department { get; set; }
    public string Designation { get; set; }
    public Dictionary<string, decimal> Components { get; set; } = new(); // clé = nom composant
    public decimal NetPay { get; set; }
    public DateTime? StartDate { get; set; }
}
