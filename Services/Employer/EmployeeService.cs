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
        // public async Task<List<Employee>> GetEmployees()
        // {
        //     var sid = GetSessionId();
        //     if (string.IsNullOrEmpty(sid))
        //     {
        //         throw new UnauthorizedAccessException("Session ID non trouvé. Veuillez vous reconnecter.");
        //     }

        //     var fields = new List<string>
        //     {
        //         "name",
        //         "employee_name",
        //         "department",
        //         "designation",
        //         "date_of_joining",
        //         "status"
        //     };
        //     var requestUrl = $"{_baseUrl}api/resource/Employee?fields={JsonSerializer.Serialize(fields)}";

        //     var request = new HttpRequestMessage(HttpMethod.Get, requestUrl)
        //     {
        //         Headers =
        //         {
        //             { "Cookie", $"sid={sid}" },
        //             { "Accept", "application/json" }
        //         }
        //     };
        //     var response = await _httpClient.SendAsync(request);
        //     var content = await response.Content.ReadAsStringAsync();
        //     var apiResponse = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(content);

        //     var result = new List<Employee>();

        //     foreach (var employee in apiResponse["data"])
        //     {
        //         var emp = new Employee
        //         {
        //             Name = employee.GetValueOrDefault("name")?.ToString(),
        //             EmployeeName = employee.GetValueOrDefault("employee_name")?.ToString(),
        //             Department = employee.GetValueOrDefault("department")?.ToString(),
        //             Designation = employee.GetValueOrDefault("designation")?.ToString(),
        //             DateOfJoining = DateTime.Parse(employee.GetValueOrDefault("date_of_joining")?.ToString() ?? DateTime.MinValue.ToString()),
        //             Status = employee.GetValueOrDefault("status")?.ToString()
        //         };
        //         result.Add(emp);
        //     }
        //     return result;
        // }
        public async Task<List<Employee>> GetEmployees(string? name = null, string? department = null, string? status = null, string? designation = null, DateTime? dateOfJoining = null)
        {
            var sid = GetSessionId();
            if (string.IsNullOrEmpty(sid))
                throw new UnauthorizedAccessException("Session ID non trouvé. Veuillez vous reconnecter.");

            var fields = new List<string> { "name", "employee_name", "department", "designation", "date_of_joining", "status" };
            
            // Construire l'URL avec filtres query string
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(name))
                queryParams.Add($"filters=[[\"employee_name\",\"like\",\"%{name}%\"]]");
            if (!string.IsNullOrEmpty(department))
                queryParams.Add($"filters=[[\"department\",\"=\",\"{department}\"]]");
            if (!string.IsNullOrEmpty(status))
                queryParams.Add($"filters=[[\"status\",\"=\",\"{status}\"]]");
            if (!string.IsNullOrEmpty(designation))
                queryParams.Add($"filters=[[\"designation\",\"=\",\"{designation}\"]]");
            if (dateOfJoining.HasValue)
                queryParams.Add($"filters=[[\"date_of_joining\",\"=\",\"{dateOfJoining.Value:yyyy-MM-dd}\"]]");

            // Fusionner les filtres : frappe API attend un seul filtre array, donc on doit concaténer en JSON
            // Exemple correct : filters=[[["field1","=","val1"],["field2","=","val2"],...]]
            // Donc on va construire un filtre JSON array contenant toutes les conditions
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
                    Status = employee.GetValueOrDefault("status")?.ToString()
                };
                result.Add(emp);
            }
            return result;
        }
    }
}