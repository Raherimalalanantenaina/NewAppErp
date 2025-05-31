using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NewAppErp.Models.Employer;
namespace NewAppErp.Services.Employer
{
    public interface IEmployeeService
    {
        Task<List<Employee>> GetEmployees();
    }
}