using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NewAppErp.Models.Salary;
namespace NewAppErp.Services.Salary.SalarySlips
{
    public class SalarySlipService : ISalarySlipService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SalarySlipService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
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
        public async Task<List<SalarySlip>> GetSalarySlipsByEmployee(string employeeId, int? month = null, int? year = null)
        {
            var sid = GetSessionId();
            if (string.IsNullOrEmpty(sid))
                throw new UnauthorizedAccessException("Session ID non trouvé.");

            var fields = new[]
            {
                "name", "employee_name", "start_date", "end_date", "net_pay", "status"
            };

            var filters = new List<object[]>
            {
                new object[] { "employee", "=", employeeId }
            };

            var url = $"{_baseUrl}api/resource/Salary Slip?fields={JsonSerializer.Serialize(fields)}&filters={Uri.EscapeDataString(JsonSerializer.Serialize(filters))}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", $"sid={sid}");
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var json = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(content);
            var result = new List<SalarySlip>();

            foreach (var item in json?["data"] ?? [])
            {
                var slip = new SalarySlip
                {
                    Name = item.GetValueOrDefault("name")?.ToString(),
                    EmployeeName = item.GetValueOrDefault("employee_name")?.ToString(),
                    StartDate = DateTime.TryParse(item.GetValueOrDefault("start_date")?.ToString(), out var start) ? start : DateTime.MinValue,
                    EndDate = DateTime.TryParse(item.GetValueOrDefault("end_date")?.ToString(), out var end) ? end : DateTime.MinValue,
                    NetPay = decimal.Parse(item.GetValueOrDefault("net_pay")?.ToString() ?? "0", CultureInfo.InvariantCulture),
                    Status = item.GetValueOrDefault("status")?.ToString()
                };

                result.Add(slip);
            }

            // Filtrage en C#
            if (year.HasValue)
            {
                result = result.Where(s => s.StartDate.Year == year.Value).ToList();
            }
            if (month.HasValue)
            {
                result = result.Where(s => s.StartDate.Month == month.Value).ToList();
            }

            return result.OrderBy(s => s.StartDate).ToList();
        }

    }
}