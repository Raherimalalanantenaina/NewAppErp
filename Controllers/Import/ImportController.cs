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
            return View(new ImportResult());
        }

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile employeeFile, IFormFile salaryFile, IFormFile salaryEmpFile)
        {
            var result = new ImportResult();

            if (employeeFile == null || salaryFile == null || salaryEmpFile == null)
            {
                ModelState.AddModelError("", "Veuillez fournir les trois fichiers.");
                return View(result);
            }

            // Traiter les fichiers CSV
            var employeeResult = await _importService.TraiterEmployeeCsvAsync(employeeFile.OpenReadStream());
            var salaryResult = await _importService.TraiterSalaryElementCsvAsync(salaryFile.OpenReadStream());
            var salaryEmpResult = await _importService.TraiterSalaryEmpCsvAsync(salaryEmpFile.OpenReadStream());

            // Regrouper les résultats
            result.ValidRows = employeeResult.ValidRows;
            result.SalaryElements = salaryResult.SalaryElements;
            result.SalaryEmpList = salaryEmpResult.SalaryEmpList;

            result.Errors.AddRange(employeeResult.Errors);
            result.Errors.AddRange(salaryResult.Errors);
            result.Errors.AddRange(salaryEmpResult.Errors);

            if (result.Errors.Any())
            {
                return View(result);
            }

            // Sinon on envoie les données à l’API
            var importData = new ImportDataDto
            {
                Employees = result.ValidRows,
                SalaryElements = result.SalaryElements,
                SalaryEmps = result.SalaryEmpList
            };

            try
            {
                var apiResponse = await _importService.EnvoyerImportDataAsync(importData);
                result.ApiMessage = apiResponse.Message;
                result.ImportSummary = apiResponse;

                // Ajouter les erreurs retournées par l’API s’il y en a
                if (apiResponse.Errors != null && apiResponse.Errors.Any())
                {
                    foreach (var err in apiResponse.Errors)
                    {
                        result.Errors.Add($"Ligne {err.Line} - Employé {err.Employee} : {err.Error}");
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Erreur lors de l’envoi des données à l’API : {ex.Message}");
            }

            return View(result);

        }
    }
}
