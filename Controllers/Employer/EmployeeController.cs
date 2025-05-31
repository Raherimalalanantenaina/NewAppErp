using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewAppErp.Models.Employer;
using NewAppErp.Services.Employer;
namespace NewAppErp.Controllers.Employer
{
    [Authorize]
    public class EmployeeController : Controller
    {
        public readonly IEmployeeService _employeeService;

        public EmployeeController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var employees = await _employeeService.GetEmployees();
            // Pagination
            var totalEmployees = employees.Count();
            var totalPages = (int)Math.Ceiling(totalEmployees / (double)pageSize);
            var pagedEmployees = employees
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // ViewBag pour la pagination
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(pagedEmployees);
        }

    }
}