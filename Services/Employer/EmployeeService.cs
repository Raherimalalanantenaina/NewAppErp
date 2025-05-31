using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Net;
using NewAppErp.Models.Employer;
namespace NewAppErp.Services.Employer
{
    public class EmployeeService : IEmployeeService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmployeeService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["NewAppErp:BaseUrl"];
            _httpContextAccessor = httpContextAccessor;
        }

        private string? GetSessionId()
        {
            return _httpContextAccessor.HttpContext?.Request.Cookies["sid"]
                ?? _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");
        }
        public async Task<List<Employee>> GetEmployees()
        {
            var sid = GetSessionId();
            if (string.IsNullOrEmpty(sid))
            {
                throw new UnauthorizedAccessException("Session ID non trouvé. Veuillez vous reconnecter.");
            }

            var fields = new List<string>
            {
                "name",
                "employee_name",
                "department",
                "designation",
                "date_of_joining",
                "status"
            };
            var requestUrl = $"{_baseUrl}api/resource/Employee?fields={JsonSerializer.Serialize(fields)}";

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl)
            {
                Headers =
                {
                    { "Cookie", $"sid={sid}" },
                    { "Accept", "application/json" }
                }
            };
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(content);

            var result = new List<Employee>();

            foreach (var employee in apiResponse["data"])
            {
                var emp = new Employee
                {
                    Name = employee.GetValueOrDefault("name")?.ToString(),
                    EmployeeName = employee.GetValueOrDefault("employee_name")?.ToString(),
                    Department = employee.GetValueOrDefault("department")?.ToString(),
                    Designation = employee.GetValueOrDefault("designation")?.ToString(),
                    DateOfJoining = DateTime.Parse(employee.GetValueOrDefault("date_of_joining")?.ToString() ?? DateTime.MinValue.ToString()),
                    Status = employee.GetValueOrDefault("status")?.ToString()
                };
                result.Add(emp);
            }
            return result;
        }
    }
}