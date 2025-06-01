using System;
namespace NewAppErp.Models.Salary;
public class SalarySlip
{
    public string Name { get; set; }
    public string Employee { get; set; }
    public string EmployeeName { get; set; }
    public string Company { get; set; }
    public string Department { get; set; }
    public string Designation { get; set; }
    public string SalaryStructure { get; set; }
    public DateTime? PostingDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Currency { get; set; }
    public decimal TotalWorkingDays { get; set; }
    public decimal PaymentDays { get; set; }
    public decimal GrossPay { get; set; }
    public decimal BaseGrossPay { get; set; }
    public decimal GrossYearToDate { get; set; }
    public decimal TotalDeduction { get; set; }
    public decimal NetPay { get; set; }
    public decimal RoundedTotal { get; set; }
    public string TotalInWords { get; set; }
    public List<SalaryComponent> Earnings { get; set; }
    public List<SalaryComponent> Deductions { get; set; }
}

