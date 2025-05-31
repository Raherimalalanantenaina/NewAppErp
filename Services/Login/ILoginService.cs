using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NewAppErp.Models.Login;
namespace NewAppErp.Services.Login
{
    public interface ILoginService
    {
        Task<string?> checkedlogin(Account user);
    }
}