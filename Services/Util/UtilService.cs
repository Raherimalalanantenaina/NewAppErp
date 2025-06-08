using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Net;
using NewAppErp.Models.Salary;

namespace NewAppErp.Services.Util
{
    public class UtilService : IUtilService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UtilService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["NewAppErp:BaseUrl"];
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<string>> GetDepartments()
        {
            return await GetDistinctValues("Department", "name");
        }

        public async Task<List<string>> GetDesignations()
        {
            return await GetDistinctValues("Designation", "name");
        }

        public Task<List<string>> GetStatuses()
        {
            return Task.FromResult(new List<string> { "Active", "Left", "Suspended" });
        }

        public async Task<List<string>> GetGenders()
        {
            return await GetDistinctValues("Gender", "name");
        }

        public async Task<List<string>> GetAllSalaryComponents()
        {
            return await GetDistinctValues("Salary Component", "name");
        }

        private string? GetSessionId()
        {
            return _httpContextAccessor.HttpContext?.Request.Cookies["sid"]
                ?? _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");
        }

        public async Task<List<string>> GetDistinctValues(string docType, string field)
        {
            var sid = GetSessionId();
            if (string.IsNullOrEmpty(sid))
                throw new UnauthorizedAccessException("Session ID non trouvé");

            var fields = new List<string> { field };
            var requestUrl = $"{_baseUrl}api/resource/{docType}?fields={JsonSerializer.Serialize(fields)}&limit_page_length=1000";

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("Cookie", $"sid={sid}");
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(content);

            var result = new List<string>();
            foreach (var item in apiResponse["data"])
            {
                var val = item.GetValueOrDefault(field)?.ToString();
                if (!string.IsNullOrEmpty(val) && !result.Contains(val))
                    result.Add(val);
            }

            return result;
        }

        public Dictionary<string, decimal> CalculerTotaux(List<EmployeeSalaryComponentGridViewModel> viewModels, List<string> componentNames)
        {
            var totals = new Dictionary<string, decimal>();
            foreach (var name in componentNames)
            {
                totals[name] = viewModels.Sum(vm => vm.Components.ContainsKey(name) ? vm.Components[name] : 0);
            }
            totals["NetPay"] = viewModels.Sum(vm => vm.NetPay);
            totals["Gain"] = viewModels.Sum(vm => vm.GrossPay);
            totals["Deduction"] = viewModels.Sum(vm => vm.TotalDeduction);
            Console.WriteLine(totals["Deduction"]);
            return totals;
        }
        public Dictionary<string, decimal> CalculerStatistique(List<MonthlySalaryComponentTotals> viewModels, List<string> componentNames)
        {
            var totals = new Dictionary<string, decimal>();
            foreach (var name in componentNames)
            {
                totals[name] = viewModels.Sum(vm => vm.Components.ContainsKey(name) ? vm.Components[name] : 0);
            }
            totals["NetPay"] = viewModels.Sum(vm => vm.NetPay);
            totals["Gain"] = viewModels.Sum(vm => vm.GrossPay);
            totals["Deduction"] = viewModels.Sum(vm => vm.TotalDeduction);
            Console.WriteLine(totals["Deduction"]);
            return totals;
        }
    }
}