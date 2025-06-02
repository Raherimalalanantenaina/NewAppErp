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


        public SalaryController(IEmployeeService employeeService, ISalarySlipService salarySlipService,IUtilService utilService)
        {
            _employeeService = employeeService;
            _salarySlipService = salarySlipService;
            _utilService = utilService;
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

        [HttpGet]
        public async Task<IActionResult> FichePaix(string name)
        {
            var slip = await _salarySlipService.GetSalarySlipDetail(name);
            return View(slip);
        }

        public async Task<IActionResult> ExportPdf(string name)
        {
            var slip =await _salarySlipService.GetSalarySlipDetail(name);
            if (slip == null) return NotFound();

            var pdfBytes = _salarySlipService.GenerateSalarySlipPdf(slip);
            return File(pdfBytes, "application/pdf", $"FichePaie_{slip.EmployeeName}.pdf");
        }
        public async Task<IActionResult> Salaireliste(int? month, int? year)
        {
            var slips = await _salarySlipService.GetSalarySlipsAsync(month, year);
            var componentNames = await _utilService.GetAllSalaryComponents(); 

            var viewModels = new List<EmployeeSalaryComponentGridViewModel>();

            foreach (var slip in slips)
            {
                var model = new EmployeeSalaryComponentGridViewModel
                {
                    EmployeeName = slip.EmployeeName,
                    Department = slip.Department,
                    Designation = slip.Designation,
                    NetPay = slip.NetPay,
                    Components = new Dictionary<string, decimal>(),
                    StartDate = slip.StartDate
                };

                // Initialise tous les composants à 0
                foreach (var name in componentNames)
                {
                    model.Components[name] = 0;
                }

                // Ajoute les valeurs des earnings
                foreach (var earning in slip.Earnings)
                {
                    if (model.Components.ContainsKey(earning.SalaryComponentName))
                        model.Components[earning.SalaryComponentName] = earning.Amount;
                }

                // Ajoute les valeurs des deductions (en négatif si tu veux)
                foreach (var deduction in slip.Deductions)
                {
                    if (model.Components.ContainsKey(deduction.SalaryComponentName))
                        model.Components[deduction.SalaryComponentName] = -deduction.Amount;
                }

                viewModels.Add(model);
            }

            var totals = _utilService.CalculerTotaux(viewModels, componentNames);
            ViewBag.ComponentNames = componentNames;
            ViewBag.ComponentTotals = totals;
            return View(viewModels);

        }

    }
}
