using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NewAppErp.Models.Salary;
namespace NewAppErp.Services.Util
{
    public interface IUtilService
    {
        Task<List<string>> GetDepartments();
        Task<List<string>> GetDesignations();
        Task<List<string>> GetStatuses();
        Task<List<string>> GetGenders();
        Task<List<string>> GetAllSalaryComponents();
        Dictionary<string, decimal> CalculerTotaux(List<EmployeeSalaryComponentGridViewModel> viewModels, List<string> componentNames);
        Dictionary<string, decimal> CalculerStatistique(List<MonthlySalaryComponentTotals> viewModels, List<string> componentNames);
    }
}