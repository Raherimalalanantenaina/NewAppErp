using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NewAppErp.Models.Login;
using System.Text;
using System.Text.Json;
using System.Net;
namespace NewAppErp.Services.Login
{
    public class LoginService : ILoginService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public LoginService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["NewAppErp:BaseUrl"];
        }
        public async Task<string?> checkedlogin(Account user)
        {
            // 1. Configurer le handler avec CookieContainer
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer()
            };
            // 2. Créer un HttpClient temporaire avec ce handler
            using var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            // 3. Envoyer la requête
            var loginEndpoint = $"{_baseUrl}api/method/login";
            var jsonContent = JsonSerializer.Serialize(user);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(loginEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                
                var cookies = handler.CookieContainer.GetCookies(new Uri(_baseUrl));
                var sidCookie = cookies["sid"];
                if (sidCookie != null)
                {
                    return sidCookie.Value;
                }
            }
            return null;
        }
    }
}