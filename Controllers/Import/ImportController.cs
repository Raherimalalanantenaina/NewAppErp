using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewAppErp.Models.ImportDto;
using NewAppErp.Services.Import;

namespace NewAppErp.Controllers.Import
{
    [Authorize]
    public class ImportController : Controller
    {
        private readonly IImportService _importService;

        public ImportController(IImportService importService)
        {
            _importService = importService;
        }

        [HttpGet]
        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile employeeFile, IFormFile salaryFile, IFormFile salaryEmpFile)
        {
            var result = new ImportResult();

            if (employeeFile == null || salaryFile == null || salaryEmpFile == null)
            {
                result.Message="Veuillez fournir les trois fichiers.";
                return View(result);
            }

            // Traiter les fichiers CSV
            var employeeResult = await _importService.TraiterEmployeeCsvAsync(employeeFile.OpenReadStream());
            var salaryResult = await _importService.TraiterSalaryElementCsvAsync(salaryFile.OpenReadStream());
            var salaryEmpResult = await _importService.TraiterSalaryEmpCsvAsync(salaryEmpFile.OpenReadStream());

            // Regrouper les résultats

            result.Errors.AddRange(employeeResult.Errors);
            result.Errors.AddRange(salaryResult.Errors);
            result.Errors.AddRange(salaryEmpResult.Errors);

            if (result.Errors.Any())
            {
                return View(result);
            }
            var data = new BulkImportDto
            {
                Employees = employeeResult.employeeImportDtos,
                SalaryElements = salaryResult.salaireElements,
                SalaryEmps = salaryEmpResult.salaryEmp
            };
            Console.WriteLine(data.Employees.Count);
            try
            {
                var apiResponse = await _importService.ImportBulkDataAsync(data);
                result.Message = apiResponse.Message;
                Console.WriteLine(apiResponse.Message);
                if (apiResponse.Errors != null && apiResponse.Errors.Any())
                {
                    foreach (var err in apiResponse.Errors)
                    {
                        result.Errors.Add(err);
                    }
                }
                else
                {
                    result.Counts = new ImportCounts
                    {
                        Employees = apiResponse.Counts.Employees,
                        Structures = apiResponse.Counts.Structures,
                        Slips=apiResponse.Counts.Slips
                    };
                }
            }
            catch (JsonException ex)
            {

                ViewBag.message = ex.Message;
                return View();
            }
            return View(result);
        }


        [HttpPost]
        public async Task<IActionResult> ResetData()
        {
            try
            {
                await _importService.ResetDataAsync();
                TempData["Message"] = "Les données ont été réinitialisées avec succès.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Erreur : {ex.Message}";
            }

            return RedirectToAction("ResetView");
        }

        [HttpGet]
        public IActionResult ResetView()
        {
            ViewBag.Message = TempData["ResetResult"];
            return View();
        }
    }
}
