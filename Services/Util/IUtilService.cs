using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
namespace NewAppErp.Services.Util
{
    public interface IUtilService
    { 
        Task<List<string>> GetDepartments();
        Task<List<string>> GetDesignations();
        Task<List<string>> GetStatuses();
    }
}