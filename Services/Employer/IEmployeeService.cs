using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NewAppErp.Models.Employer;
using NewAppErp.Models.Salary;
namespace NewAppErp.Services.Employer
{
    public interface IEmployeeService
    {
        //Task<List<Employee>> GetEmployees();
        Task<List<Employee>> GetEmployees(string? name = null, string? department = null, string? status = null, string? designation = null, DateTime? dateOfJoining = null, string? gender = null);
        Task<Employee?> GetEmployeeById(string id);

    }
}