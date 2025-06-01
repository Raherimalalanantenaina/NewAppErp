using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewAppErp.Models.Employer;
using NewAppErp.Services.Employer;
using NewAppErp.Services.Salary.SalarySlips;

namespace NewAppErp.Controllers.Salary
{
    [Authorize]
    public class SalaryController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly ISalarySlipService _salarySlipService;

        public SalaryController(IEmployeeService employeeService, ISalarySlipService salarySlipService)
        {
            _employeeService = employeeService;
            _salarySlipService = salarySlipService;
        }
        [HttpGet]
        public async Task<IActionResult> Index(string employeeId, int? mois, int? annee, int page = 1, int pageSize = 10)
        {
            try
            {
                var employee = await _employeeService.GetEmployeeById(employeeId);
                var salaries = await _salarySlipService.GetSalarySlipsByEmployee(employeeId, mois, annee);

                int totalItems = salaries.Count;
                int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                var pagedSalaries = salaries
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.EmployeeId = employeeId;
                ViewBag.Mois = mois;
                ViewBag.Annee = annee;

                var fiche = new EmployeeSalaryInfo
                {
                    Employee = employee,
                    Salaries = pagedSalaries
                };

                return View(fiche);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur fiche employé : {ex.Message}");
                return View("Error", ex.Message);
            }
        }


    }
}
