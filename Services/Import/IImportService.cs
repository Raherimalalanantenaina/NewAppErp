namespace NewAppErp.Services.Import;
using System.Globalization;
using NewAppErp.Models.ImportDto;

public interface IImportService
{
    Task<ImportResult> TraiterEmployeeCsvAsync(Stream fileStream);
    Task<ImportResult> TraiterSalaryElementCsvAsync(Stream fileStream);
    Task<ImportResult> TraiterSalaryEmpCsvAsync(Stream fileStream);
    Task<ImportResult> ImportBulkDataAsync(BulkImportDto data);
    //Task<ImportResponseDto> EnvoyerImportDataAsync(ImportDataDto importData);
    Task<bool> ResetDataAsync();
}