using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NewAppErp.Models.Salary;
namespace NewAppErp.Services.Salary.SalarySlips
{
    public interface ISalarySlipService
    {
        Task<List<SalarySlip>> GetSalarySlipsByEmployee(string employeeId, int? month = null, int? year = null);
        Task<SalarySlip> GetSalarySlipDetail(string name);
        byte[] GenerateSalarySlipPdf(SalarySlip slip);
        Task<List<SalarySlip>> GetSalarySlipsAsync(int? month, int? year);
    }
}