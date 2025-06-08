using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewAppErp.Models.Employer;
using NewAppErp.Models.Salary;
using NewAppErp.Services.Employer;
using NewAppErp.Services.Salary.SalarySlips;
using NewAppErp.Services.Util;

namespace NewAppErp.Controllers.Salary
{
    [Authorize]
    public class SalaryController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly ISalarySlipService _salarySlipService;

        private readonly IUtilService _utilService;


        public SalaryController(IEmployeeService employeeService, ISalarySlipService salarySlipService, IUtilService utilService)
        {
            _employeeService = employeeService;
            _salarySlipService = salarySlipService;
            _utilService = utilService;
        }
        [HttpGet]
        public async Task<IActionResult> Index(string employeeId, int? mois, int? annee, int page = 1)
        {
            try
            {
                int pageSize = 10;
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

        [HttpGet]
        public async Task<IActionResult> FichePaix(string name)
        {
            var slip = await _salarySlipService.GetSalarySlipDetail(name);
            return View(slip);
        }

        public async Task<IActionResult> ExportPdf(string name)
        {
            var slip = await _salarySlipService.GetSalarySlipDetail(name);
            if (slip == null) return NotFound();

            var pdfBytes = _salarySlipService.GenerateSalarySlipPdf(slip);
            return File(pdfBytes, "application/pdf", $"FichePaie_{slip.EmployeeName}.pdf");
        }
        public async Task<IActionResult> Salaireliste(int? mois, int page = 1)
        {
            int pageSize = 10;
            var slips = await _salarySlipService.GetSalarySlipsAsync(mois, null);
            var componentNames = await _utilService.GetAllSalaryComponents();

            var viewModels = await _salarySlipService.BuildEmployeeSalaryViewModelsMois(slips, componentNames);
            int totalItems = viewModels.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var pagedSalaries = viewModels
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var totals = _utilService.CalculerTotaux(viewModels, componentNames);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.ComponentNames = componentNames;
            ViewBag.ComponentTotals = totals;
            ViewBag.Mois = mois;
            return View(pagedSalaries);
        }

        public async Task<IActionResult> SalairelisteEmp(int? mois, int? annee, int page = 1)
        {
            int pageSize = 10;
            var slips = await _salarySlipService.GetSalarySlipsAsync(mois, annee);
            var componentNames = await _utilService.GetAllSalaryComponents();

            var viewModels = await _salarySlipService.BuildEmployeeSalaryViewModelsAsync(slips, componentNames);
            int totalItems = viewModels.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var pagedSalaries = viewModels
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var totals = _utilService.CalculerTotaux(viewModels, componentNames);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.ComponentNames = componentNames;
            ViewBag.ComponentTotals = totals;
            ViewBag.Mois = mois;
            ViewBag.Annee = annee;
            return View(pagedSalaries);
        }

        public async Task<IActionResult> SalaireStatistique(int? annee)
        {
            var slips = await _salarySlipService.GetSalarySlipsAsync(null, annee);
            var componentNames = await _utilService.GetAllSalaryComponents();

            var totals = await _salarySlipService.GetMonthlySalaryComponentTotalsAsync(slips, componentNames);
            var totalcalcul = _utilService.CalculerStatistique(totals,componentNames);
            ViewBag.ComponentNames = componentNames;
            ViewBag.ComponentTotals = totalcalcul;
            ViewBag.Annee = annee;
            return View(totals);
        }
        
        public async Task<IActionResult> SalaireChart(int? annee)
        {
            var slips = await _salarySlipService.GetSalarySlipsAsync(null, annee);
            var componentNames = await _utilService.GetAllSalaryComponents();
            ViewBag.Annee = annee;
            ViewBag.ComponentNames = componentNames;
            var data = await _salarySlipService.GetSalaryChartDataAsync(slips,componentNames);
            return View(data);
        }

    }
}
