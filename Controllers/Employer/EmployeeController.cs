using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewAppErp.Models.Employer;
using NewAppErp.Services.Employer;
using NewAppErp.Services.Util;

namespace NewAppErp.Controllers.Employer
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly IUtilService _utilService;

        public EmployeeController(IEmployeeService employeeService, IUtilService utilService)
        {
            _employeeService = employeeService;
            _utilService = utilService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 10,
            string? name = null,
            string? department = null,
            string? status = null,
            string? designation = null,
            DateTime? dateOfJoining = null)
        {
            try
            {
                var employees = await _employeeService.GetEmployees(name, department, status, designation, dateOfJoining);

                // Pagination
                var totalEmployees = employees.Count();
                var totalPages = (int)Math.Ceiling(totalEmployees / (double)pageSize);
                var pagedEmployees = employees
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // ViewBag pour la pagination et les filtres
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.Departments = await _utilService.GetDepartments();
                ViewBag.Designations = await _utilService.GetDesignations();
                ViewBag.StatusList = await _utilService.GetStatuses();

                // Renvoyer les valeurs des filtres à la vue pour les pré-remplir
                ViewBag.FilterName = name;
                ViewBag.FilterDepartment = department;
                ViewBag.FilterStatus = status;
                ViewBag.FilterDesignation = designation;
                ViewBag.FilterDateOfJoining = dateOfJoining?.ToString("yyyy-MM-dd");

                return View(pagedEmployees);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur dans EmployeeController.Index : {ex.Message}");
                return View("Error", ex.Message);
            }
        }
    }
}
