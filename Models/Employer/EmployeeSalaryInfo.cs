using System;
using NewAppErp.Models.Salary;
namespace NewAppErp.Models.Employer
{ 
    public class EmployeeSalaryInfo
    {
        public Employee? Employee { get; set; }
        public List<SalarySlip> Salaries { get; set; } = new();
    }

}