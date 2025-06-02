using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Net;
using NewAppErp.Models.Employer;
using NewAppErp.Models.Salary;
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

        public async Task<List<Employee>> GetEmployees(string? name = null, string? department = null, string? status = null, string? designation = null, DateTime? dateOfJoining = null, string? gender = null)
        {
            var sid = GetSessionId();
            if (string.IsNullOrEmpty(sid))
                throw new UnauthorizedAccessException("Session ID non trouvé. Veuillez vous reconnecter.");

            var fields = new List<string>
            {
                "name", "employee_name", "department", "designation", "date_of_joining", "status", "gender" ,"date_of_birth"
            };

            // Construction des filtres
            var filtersList = new List<object[]>();

            if (!string.IsNullOrEmpty(name))
                filtersList.Add(new object[] { "employee_name", "like", $"%{name}%" });
            if (!string.IsNullOrEmpty(department))
                filtersList.Add(new object[] { "department", "=", department });
            if (!string.IsNullOrEmpty(status))
                filtersList.Add(new object[] { "status", "=", status });
            if (!string.IsNullOrEmpty(designation))
                filtersList.Add(new object[] { "designation", "=", designation });
            if (dateOfJoining.HasValue)
                filtersList.Add(new object[] { "date_of_joining", "=", dateOfJoining.Value.ToString("yyyy-MM-dd") });
            if (!string.IsNullOrEmpty(gender))
                filtersList.Add(new object[] { "gender", "=", gender });

            string filtersJson = JsonSerializer.Serialize(filtersList);

            var requestUrl = $"{_baseUrl}api/resource/Employee?fields={JsonSerializer.Serialize(fields)}";

            if (filtersList.Count > 0)
                requestUrl += $"&filters={Uri.EscapeDataString(filtersJson)}";

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("Cookie", $"sid={sid}");
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(content);

            var result = new List<Employee>();

            foreach (var employee in apiResponse?["data"] ?? new List<Dictionary<string, object>>())
            {
                var emp = new Employee
                {
                    Name = employee.GetValueOrDefault("name")?.ToString(),
                    EmployeeName = employee.GetValueOrDefault("employee_name")?.ToString(),
                    Department = employee.GetValueOrDefault("department")?.ToString(),
                    Designation = employee.GetValueOrDefault("designation")?.ToString(),
                    DateOfJoining = DateTime.TryParse(employee.GetValueOrDefault("date_of_joining")?.ToString(), out var dt) ? dt : DateTime.MinValue,
                    Status = employee.GetValueOrDefault("status")?.ToString(),
                    Gender = employee.GetValueOrDefault("gender")?.ToString(),  // Ajout du genre ici
                    DateOfBirth = DateTime.TryParse(employee.GetValueOrDefault("date_of_birth")?.ToString(), out var dob) ? dob : DateTime.MinValue
                };
                result.Add(emp);
            }
            return result;
        }
        public async Task<Employee?> GetEmployeeById(string id)
        {
            var sid = GetSessionId();
            if (string.IsNullOrEmpty(sid))
                throw new UnauthorizedAccessException("Session ID non trouvé.");

            var url = $"{_baseUrl}api/resource/Employee/{id}?fields=[\"name\",\"employee_name\",\"department\",\"designation\",\"date_of_joining\",\"status\",\"gender\",\"date_of_birth\"]";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", $"sid={sid}");
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var json = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(content);

            var data = json?["data"];
            if (data == null) return null;

            return new Employee
            {
                Name = data.GetValueOrDefault("name")?.ToString(),
                EmployeeName = data.GetValueOrDefault("employee_name")?.ToString(),
                Department = data.GetValueOrDefault("department")?.ToString(),
                Designation = data.GetValueOrDefault("designation")?.ToString(),
                DateOfJoining = DateTime.TryParse(data.GetValueOrDefault("date_of_joining")?.ToString(), out var dt) ? dt : DateTime.MinValue,
                Status = data.GetValueOrDefault("status")?.ToString(),
                Gender = data.GetValueOrDefault("gender")?.ToString(),
                DateOfBirth = DateTime.TryParse(data.GetValueOrDefault("date_of_birth")?.ToString(), out var dob) ? dob : DateTime.MinValue

            };
        }


    }
}