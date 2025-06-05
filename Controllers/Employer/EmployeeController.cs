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

    public async Task<IActionResult> Index(string? name = null, string? department = null, string? status = null, string? designation = null, DateTime? dateOfJoining = null, string? gender = null, int page = 1)
    {
        try
        {
            int pageSize = 10;
            int offset = (page - 1) * pageSize;

            var pagedResult = await _employeeService.GetEmployees(name, department, status, designation, dateOfJoining, gender, pageSize, offset);

            ViewBag.Departments = await _utilService.GetDepartments();
            ViewBag.Designations = await _utilService.GetDesignations();
            ViewBag.StatusList = await _utilService.GetStatuses();
            ViewBag.GenderList = await _utilService.GetGenders();

            // Filtres
            ViewBag.FilterName = name;
            ViewBag.FilterDepartment = department;
            ViewBag.FilterStatus = status;
            ViewBag.FilterDesignation = designation;
            ViewBag.FilterDateOfJoining = dateOfJoining?.ToString("yyyy-MM-dd");
            ViewBag.FilterGender = gender;

            // Pagination
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = pagedResult.TotalCount;
            ViewBag.TotalPages = (int)Math.Ceiling(pagedResult.TotalCount / (double)pageSize);

            return View(pagedResult.Items);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erreur dans EmployeeController.Index : {ex.Message}");
            return View("Error", ex.Message);
        }
    }



    }
}
