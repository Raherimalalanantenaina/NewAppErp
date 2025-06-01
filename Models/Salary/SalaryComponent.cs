using System;
namespace NewAppErp.Models.Salary;
public class SalaryComponent
{
    public string SalaryComponentName { get; set; }
    public string Abbreviation { get; set; }
    public decimal Amount { get; set; }
    public decimal YearToDate { get; set; }
}
