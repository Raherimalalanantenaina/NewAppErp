using System.Globalization;
using System.Text;
using System.Text.Json;
using NewAppErp.Models.ImportDto;
using Newtonsoft.Json.Linq;

namespace NewAppErp.Services.Import;

public class ImportService : IImportService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly IHttpContextAccessor _httpContextAccessor;
     private readonly ILogger<ImportService> _logger;

    public ImportService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<ImportService> logger)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["NewAppErp:BaseUrl"];
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private string? GetSessionId()
    {
        return _httpContextAccessor.HttpContext?.Request.Cookies["sid"]
            ?? _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");
    }

    public async Task<ImportResult> TraiterEmployeeCsvAsync(Stream fileStream)
    {
        var result = new ImportResult();
        using var reader = new StreamReader(fileStream);
        int lineIndex = 0;
        string[]? headers = null;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            lineIndex++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var columns = line.Split(',');

            if (lineIndex == 1)
            {
                headers = columns;
                if (headers.Length < 7)
                    result.Errors.Add("Employé - Ligne 1 : En-têtes employés invalides.");
                continue;
            }

            var errors = new List<string>();
            var dto = new EmployeeImportDto();

            if (columns.Length < 7)
            {
                result.Errors.Add($"Employé - Ligne {lineIndex} : Colonnes manquantes.");
                continue;
            }

            dto.Ref = columns[0];
            dto.Nom = columns[1];
            dto.Prenom = columns[2];
            dto.Genre = columns[3];
            dto.Company = columns[6];

            bool embaucheValide = DateTime.TryParseExact(columns[4], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateEmbauche);
            bool naissanceValide = DateTime.TryParseExact(columns[5], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateNaissance);

            if (!embaucheValide)
                errors.Add("Date d'embauche invalide");
            else
                dto.DateEmbauche = dateEmbauche;

            if (!naissanceValide)
                errors.Add("Date de naissance invalide");
            else
                dto.DateNaissance = dateNaissance;

            if (embaucheValide && naissanceValide && dto.DateEmbauche <= dto.DateNaissance)
                errors.Add("Date d'embauche doit être supérieure à la date de naissance");

            if (errors.Any())
                result.Errors.Add($"Employé - Ligne {lineIndex} : {string.Join(" ; ", errors)}");
            else
                result.ValidRows.Add(dto);
        }

        return result;
    }

    public async Task<ImportResult> TraiterSalaryElementCsvAsync(Stream fileStream)
    {
        var result = new ImportResult();
        using var reader = new StreamReader(fileStream);
        int lineIndex = 0;
        string[]? headers = null;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            lineIndex++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var columns = line.Split(',');

            if (lineIndex == 1)
            {
                headers = columns;
                if (headers.Length < 6)
                    result.Errors.Add("SalaireElement - Ligne 1 : En-têtes invalides.");
                continue;
            }

            var errors = new List<string>();
            if (columns.Length < 6)
            {
                result.Errors.Add($"SalaireElement - Ligne {lineIndex} : Colonnes manquantes.");
                continue;
            }

            var dto = new SalaryElementImportDto
            {
                SalaryStructure = columns[0],
                Name = columns[1],
                Abbr = columns[2],
                Type = columns[3],
                Valeur = columns[4],
                Company = columns[5]
            };

            if (string.IsNullOrWhiteSpace(dto.SalaryStructure)) errors.Add("Structure manquante");
            if (string.IsNullOrWhiteSpace(dto.Name)) errors.Add("Nom manquant");
            if (string.IsNullOrWhiteSpace(dto.Abbr)) errors.Add("Abréviation manquante");
            if (string.IsNullOrWhiteSpace(dto.Type)) errors.Add("Type manquant");
            if (string.IsNullOrWhiteSpace(dto.Valeur)) errors.Add("Valeur manquante");

            if (errors.Any())
                result.Errors.Add($"SalaireElement - Ligne {lineIndex} : {string.Join(" ; ", errors)}");
            else
                result.SalaryElements.Add(dto);
        }

        return result;
    }

    public async Task<ImportResult> TraiterSalaryEmpCsvAsync(Stream fileStream)
    {
        var result = new ImportResult();
        using var reader = new StreamReader(fileStream);
        int lineIndex = 0;
        string[]? headers = null;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            lineIndex++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var columns = line.Split(',');

            if (lineIndex == 1)
            {
                headers = columns;
                if (headers.Length < 4)
                    result.Errors.Add("SalaireEmployé - Ligne 1 : En-têtes invalides.");
                continue;
            }

            var errors = new List<string>();
            if (columns.Length < 4)
            {
                result.Errors.Add($"SalaireEmployé - Ligne {lineIndex} : Colonnes manquantes.");
                continue;
            }

            var dto = new SalaryEmpImportDto();

            if (!DateTime.TryParseExact(columns[0], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var mois))
                errors.Add("Date du mois invalide");
            else
                dto.Mois = mois;

            dto.RefEmploye = columns[1];

            if (!decimal.TryParse(columns[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var salaireBase))
                errors.Add("Salaire de base invalide");
            else
                dto.SalaireBase = salaireBase;

            dto.Salaire = columns[3];

            if (errors.Any())
                result.Errors.Add($"SalaireEmployé - Ligne {lineIndex} : {string.Join(" ; ", errors)}");
            else
                result.SalaryEmpList.Add(dto);
        }

        return result;
    }

    public async Task<ImportResponseDto> EnvoyerImportDataAsync(ImportDataDto importData)
    {
        try
        {
            var sid = GetSessionId();
            var requestUrl = $"{_baseUrl}api/method/import_app.api.traitementdata.import_bulk_data";

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(importData, jsonOptions), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Cookie", $"sid={sid}");
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            var json = JObject.Parse(content)["message"];

            var importreponse = new ImportResponseDto
            {
                Success = json["success"]?.Value<bool>() ?? false,
                Message = json["message"]?.ToString(),
                Counts = json["counts"] != null ? new ImportCountsDto
                {
                    Employees = json["counts"]["employees"]?.Value<int>() ?? 0,
                    Structures = json["counts"]["structures"]?.Value<int>() ?? 0,
                    Slips = json["counts"]["slips"]?.Value<int>() ?? 0
                } : null,
                Errors = json["errors"] != null
                    ? json["errors"].Select(e => e.ToString()).ToList()
                    : new List<string>()
            };

            return importreponse;
        }
        catch (Exception ex)
        {
            throw new Exception("Erreur lors de l'importation", ex);
        }
    }

    public async Task<bool> ResetDataAsync()
    {
        var sid = GetSessionId();
        var requestUrl = $"{_baseUrl}api/method/import_app.api.reset_data.reset_data";

        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Add("Cookie", $"sid={sid}");
        request.Headers.Add("Accept", "application/json");

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Erreur API: {response.StatusCode} - {content}");

        return true;
    }

}
