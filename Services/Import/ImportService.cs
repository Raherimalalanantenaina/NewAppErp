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
                Console.WriteLine("miditra");
                result.employeeImportDtos.Add(dto);
        }

        return result;
    }


    public async Task<ImportResult> TraiterSalaryElementCsvAsync(Stream fileStream)
    {
        var result = new ImportResult();
        using var reader = new StreamReader(fileStream);
        int lineIndex = 0;
        string[]? headers = null;

        var salaryElement = new List<SalaryElementImportDto>();

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
                    result.Errors.Add("ÉlémentSalaire - Ligne 1 : En-têtes invalides.");
                continue;
            }

            if (columns.Length < 6)
            {
                result.Errors.Add($"ÉlémentSalaire - Ligne {lineIndex} : Colonnes manquantes.");
                continue;
            }

            var errors = new List<string>();
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
                result.Errors.Add($"ÉlémentSalaire - Ligne {lineIndex} : {string.Join(" ; ", errors)}");
            else
                salaryElement.Add(dto);
                result.Counts.Structures++;
        }

        result.Success = result.Errors.Count == 0;
        result.salaireElements = salaryElement;
        result.Message = result.Success
            ? "Import des éléments de salaire réussi."
            : "Import des éléments de salaire terminé avec des erreurs.";

        return result;
    }

    public async Task<ImportResult> TraiterSalaryEmpCsvAsync(Stream fileStream)
    {
        var result = new ImportResult();
        using var reader = new StreamReader(fileStream);
        int lineIndex = 0;
        string[]? headers = null;

        var salaryEmpList = new List<SalaryEmpImportDto>();

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

            if (columns.Length < 4)
            {
                result.Errors.Add($"SalaireEmployé - Ligne {lineIndex} : Colonnes manquantes.");
                continue;
            }

            var errors = new List<string>();
            var dto = new SalaryEmpImportDto();

            if (!DateTime.TryParseExact(columns[0], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var mois))
                errors.Add("Date du mois invalide");
            else
                dto.Mois = mois;

            dto.RefEmploye = columns[1];

            if (!decimal.TryParse(columns[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var salaireBase))
                errors.Add("Salaire de base invalide");
            else
                dto.SalaireBase = salaireBase;

            dto.Salaire = columns[3];

            if (string.IsNullOrWhiteSpace(dto.RefEmploye))
                errors.Add("Référence employé manquante");

            if (errors.Any())
            {
                result.Errors.Add($"SalaireEmployé - Ligne {lineIndex} : {string.Join(" ; ", errors)}");
            }
            else
            {
                salaryEmpList.Add(dto);
            }
        }

        result.Success = !result.Errors.Any();
        result.salaryEmp = salaryEmpList;
        result.Counts.Slips = salaryEmpList.Count;
        return result;
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


    public async Task<string> EnsureFiscalYearForDateAsync(DateTime date)
    {
        try
        {
            Console.WriteLine($"Vérification année fiscale pour date : {date:yyyy-MM-dd}");

            var sid = GetSessionId();
            if (sid == null)
                throw new Exception("Session invalide");

            // Vérifier si une année fiscale existe déjà
            var filter = new
            {
                disabled = 0,
                year_start_date = new[] { "<=", date.ToString("yyyy-MM-dd") },
                year_end_date = new[] { ">=", date.ToString("yyyy-MM-dd") }
            };

            var url = $"{_baseUrl}api/resource/Fiscal Year?filters={Uri.EscapeDataString(JsonSerializer.Serialize(filter))}&fields=[\"name\"]";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", $"sid={sid}");
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Erreur récupération année fiscale : {response.StatusCode} - {content}");

            var json = JsonSerializer.Deserialize<JsonElement>(content);
            var data = json.GetProperty("data");

            if (data.GetArrayLength() > 0)
            {
                var fiscalYear = data[0].GetProperty("name").GetString();
                Console.WriteLine($"Année fiscale trouvée : {fiscalYear}");
                return fiscalYear;
            }

            // Aucune année fiscale, création
            Console.WriteLine($"Aucune année fiscale existante, création pour l'année : {date.Year}");

            var createPayload = new Dictionary<string, object>
            {
                ["doctype"] = "Fiscal Year",
                ["year_start_date"] = new DateTime(date.Year, 1, 1).ToString("yyyy-MM-dd"),
                ["year_end_date"] = new DateTime(date.Year, 12, 31).ToString("yyyy-MM-dd"),
                ["year"] = date.Year.ToString(),
                ["skip_salary_structure_creation"] = true
            };

            var createRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}api/resource/Fiscal Year")
            {
                Content = new StringContent(JsonSerializer.Serialize(new { data = createPayload }), Encoding.UTF8, "application/json")
            };
            createRequest.Headers.Add("Cookie", $"sid={sid}");
            createRequest.Headers.Add("Accept", "application/json");

            var createResponse = await _httpClient.SendAsync(createRequest);
            var createContent = await createResponse.Content.ReadAsStringAsync();

            if (!createResponse.IsSuccessStatusCode)
                throw new Exception($"Erreur création année fiscale : {createResponse.StatusCode} - {createContent}");

            var createdJson = JsonSerializer.Deserialize<JsonElement>(createContent);
            var createdName = createdJson.GetProperty("data").GetProperty("name").GetString();

            Console.WriteLine($"Nouvelle année fiscale créée : {createdName}");
            return createdName;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur dans EnsureFiscalYearForDateAsync : {ex.Message}");
            return null;
        }
    }



    public async Task<string?> EnsureDefaultHolidayListAsync()
    {
        const string name = "Default Holidays";
        var startDate = new DateTime(1900, 1, 1);
        var endDate = new DateTime(2100, 12, 31);

        var sid = GetSessionId();
        if (sid == null)
        {
            _logger.LogWarning("Session ID introuvable, authentification requise.");
            return null;
        }

        // Vérifier si le Holiday List existe déjà
        var requestCheck = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}api/resource/Holiday List/{Uri.EscapeDataString(name)}");
        requestCheck.Headers.Add("Cookie", $"sid={sid}");
        requestCheck.Headers.Add("Accept", "application/json");

        var responseCheck = await _httpClient.SendAsync(requestCheck);
        if (responseCheck.IsSuccessStatusCode)
        {
            // Holiday List existe, on met à jour from_date et to_date
            var contentCheck = await responseCheck.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(contentCheck);
            var data = jsonDoc.RootElement.GetProperty("data");

            var patchPayload = new
            {
                from_date = startDate.ToString("yyyy-MM-dd"),
                to_date = endDate.ToString("yyyy-MM-dd")
            };

            var requestUpdate = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}api/resource/Holiday List/{Uri.EscapeDataString(name)}")
            {
                Content = new StringContent(JsonSerializer.Serialize(patchPayload), Encoding.UTF8, "application/json")
            };
            requestUpdate.Headers.Add("Cookie", $"sid={sid}");
            requestUpdate.Headers.Add("Accept", "application/json");

            var responseUpdate = await _httpClient.SendAsync(requestUpdate);
            if (!responseUpdate.IsSuccessStatusCode)
            {
                var err = await responseUpdate.Content.ReadAsStringAsync();
                _logger.LogError("Erreur lors de la mise à jour de Holiday List : {StatusCode} - {Content}", responseUpdate.StatusCode, err);
                return null;
            }

            _logger.LogInformation("Holiday List existante mise à jour : {Name}", name);
            return name;
        }
        else if (responseCheck.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Holiday List n'existe pas, on crée
            var newDoc = new
            {
                holiday_list_name = name,
                from_date = startDate.ToString("yyyy-MM-dd"),
                to_date = endDate.ToString("yyyy-MM-dd"),
                holidays = new[]
                {
                    new {
                        holiday_date = startDate.ToString("yyyy-MM-dd"),
                        description = "New Year"
                    }
                }
            };

            var requestCreate = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}api/resource/Holiday List")
            {
                Content = new StringContent(JsonSerializer.Serialize(new { data = newDoc }), Encoding.UTF8, "application/json")
            };
            requestCreate.Headers.Add("Cookie", $"sid={sid}");
            requestCreate.Headers.Add("Accept", "application/json");

            var responseCreate = await _httpClient.SendAsync(requestCreate);
            if (!responseCreate.IsSuccessStatusCode)
            {
                var err = await responseCreate.Content.ReadAsStringAsync();
                _logger.LogError("Erreur lors de la création de Holiday List : {StatusCode} - {Content}", responseCreate.StatusCode, err);
                return null;
            }

            var contentCreate = await responseCreate.Content.ReadAsStringAsync();
            using var jsonDocCreate = JsonDocument.Parse(contentCreate);
            var createdName = jsonDocCreate.RootElement.GetProperty("data").GetProperty("name").GetString();
            _logger.LogInformation("Nouvelle Holiday List créée : {Name}", createdName);
            return createdName;
        }
        else
        {
            var err = await responseCheck.Content.ReadAsStringAsync();
            _logger.LogError("Erreur lors de la récupération de Holiday List : {StatusCode} - {Content}", responseCheck.StatusCode, err);
            return null;
        }
    }


    public async Task EnsureCompanyAsync(string name)
    {
        var sid = GetSessionId();
        if (sid == null)
        {
            _logger.LogWarning("Session ID introuvable, authentification requise.");
            return;
        }

        // Vérifier si la société existe
        var requestCheck = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}api/resource/Company/{Uri.EscapeDataString(name)}");
        requestCheck.Headers.Add("Cookie", $"sid={sid}");
        requestCheck.Headers.Add("Accept", "application/json");

        var responseCheck = await _httpClient.SendAsync(requestCheck);

        if (!responseCheck.IsSuccessStatusCode && responseCheck.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            var err = await responseCheck.Content.ReadAsStringAsync();
            _logger.LogError("Erreur lors de la récupération de la société : {StatusCode} - {Content}", responseCheck.StatusCode, err);
            return;
        }

        var defaultHolidayList = await EnsureDefaultHolidayListAsync();
        if (defaultHolidayList == null)
        {
            _logger.LogError("Impossible d'assurer la liste des jours fériés par défaut.");
            return;
        }

        if (responseCheck.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Société n'existe pas, on crée
            var newCompany = new
            {
                company_name = name,
                default_currency = "USD",
                default_holiday_list = defaultHolidayList
            };

            var requestCreate = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}api/resource/Company")
            {
                Content = new StringContent(JsonSerializer.Serialize(new { data = newCompany }), Encoding.UTF8, "application/json")
            };
            requestCreate.Headers.Add("Cookie", $"sid={sid}");
            requestCreate.Headers.Add("Accept", "application/json");

            var responseCreate = await _httpClient.SendAsync(requestCreate);
            if (!responseCreate.IsSuccessStatusCode)
            {
                var err = await responseCreate.Content.ReadAsStringAsync();
                _logger.LogError("Erreur lors de la création de la société : {StatusCode} - {Content}", responseCreate.StatusCode, err);
            }
            else
            {
                _logger.LogInformation("Société créée : {Name}", name);
            }
        }
        else
        {
            // Société existe, vérifier et mettre à jour default_holiday_list si nécessaire
            var contentCheck = await responseCheck.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(contentCheck);
            var companyData = jsonDoc.RootElement.GetProperty("data");

            if (!companyData.TryGetProperty("default_holiday_list", out var holidayListProp) || holidayListProp.GetString() == null)
            {
                var patchPayload = new
                {
                    default_holiday_list = defaultHolidayList
                };

                var requestUpdate = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}api/resource/Company/{Uri.EscapeDataString(name)}")
                {
                    Content = new StringContent(JsonSerializer.Serialize(patchPayload), Encoding.UTF8, "application/json")
                };
                requestUpdate.Headers.Add("Cookie", $"sid={sid}");
                requestUpdate.Headers.Add("Accept", "application/json");

                var responseUpdate = await _httpClient.SendAsync(requestUpdate);
                if (!responseUpdate.IsSuccessStatusCode)
                {
                    var err = await responseUpdate.Content.ReadAsStringAsync();
                    _logger.LogError("Erreur lors de la mise à jour de la société : {StatusCode} - {Content}", responseUpdate.StatusCode, err);
                }
                else
                {
                    _logger.LogInformation("Société mise à jour avec default_holiday_list : {Name}", name);
                }
            }
        }
    }

    public async Task<string> UpsertSalarySlipAsync(Dictionary<string, object> slipData, string employeeName, string structureName)
{
    var sid = GetSessionId();
    if (sid == null)
        throw new Exception("Session invalide");

    var fromDate = DateTime.Parse(slipData["mois"].ToString());
    var toDate = fromDate.AddDays(DaysInMonth(fromDate) - 1);

    await EnsureFiscalYearForDateAsync(fromDate);

    // Vérifier l'existence d'une affectation de structure salariale
    var filter = new
    {
        employee = employeeName,
        salary_structure = structureName,
        from_date = new[] { "<=", fromDate.ToString("yyyy-MM-dd") },
        to_date = new[] { "=", (string)null }
    };

    var checkUrl = $"{_baseUrl}api/resource/Salary Structure Assignment?filters={Uri.EscapeDataString(JsonSerializer.Serialize(filter))}&fields=[\"name\"]";
    var checkRequest = new HttpRequestMessage(HttpMethod.Get, checkUrl);
    checkRequest.Headers.Add("Cookie", $"sid={sid}");
    checkRequest.Headers.Add("Accept", "application/json");

    var checkResponse = await _httpClient.SendAsync(checkRequest);
    var checkContent = await checkResponse.Content.ReadAsStringAsync();

    var assignmentExists = false;
    if (checkResponse.IsSuccessStatusCode)
    {
        var checkJson = JsonSerializer.Deserialize<JsonElement>(checkContent);
        var data = checkJson.GetProperty("data");
        assignmentExists = data.GetArrayLength() > 0;
    }

    if (!assignmentExists)
    {
        var assignmentPayload = new Dictionary<string, object>
        {
            ["doctype"] = "Salary Structure Assignment",
            ["employee"] = employeeName,
            ["salary_structure"] = structureName,
            ["from_date"] = fromDate.ToString("yyyy-MM-dd"),
            ["base"] = Convert.ToDouble(slipData.GetValueOrDefault("salaireBase", 0))
        };

        var assignRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}api/resource/Salary Structure Assignment")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { data = assignmentPayload }), Encoding.UTF8, "application/json")
        };
        assignRequest.Headers.Add("Cookie", $"sid={sid}");
        assignRequest.Headers.Add("Accept", "application/json");

        var assignResponse = await _httpClient.SendAsync(assignRequest);
        var assignContent = await assignResponse.Content.ReadAsStringAsync();

        if (!assignResponse.IsSuccessStatusCode)
            throw new Exception($"Erreur création SSA : {assignResponse.StatusCode} - {assignContent}");

        // Soumettre l'Assignment
        var createdSSA = JsonSerializer.Deserialize<JsonElement>(assignContent).GetProperty("data");
        var ssaDoc = createdSSA;

        var submitSSA = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}api/method/frappe.client.submit")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { doc = ssaDoc }), Encoding.UTF8, "application/json")
        };
        submitSSA.Headers.Add("Cookie", $"sid={sid}");
        submitSSA.Headers.Add("Accept", "application/json");

        var submitSSAResponse = await _httpClient.SendAsync(submitSSA);
        var submitSSAContent = await submitSSAResponse.Content.ReadAsStringAsync();

        if (!submitSSAResponse.IsSuccessStatusCode)
            throw new Exception($"Erreur soumission SSA : {submitSSAResponse.StatusCode} - {submitSSAContent}");
    }

    // Création du Salary Slip
    var slipPayload = new Dictionary<string, object>
    {
        ["doctype"] = "Salary Slip",
        ["employee"] = employeeName,
        ["salary_structure"] = structureName,
        ["start_date"] = fromDate.ToString("yyyy-MM-dd"),
        ["end_date"] = toDate.ToString("yyyy-MM-dd"),
        ["posting_date"] = toDate.ToString("yyyy-MM-dd"),
        ["company"] = slipData.GetValueOrDefault("company")?.ToString()
    };

    var slipRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}api/resource/Salary Slip")
    {
        Content = new StringContent(JsonSerializer.Serialize(new { data = slipPayload }), Encoding.UTF8, "application/json")
    };
    slipRequest.Headers.Add("Cookie", $"sid={sid}");
    slipRequest.Headers.Add("Accept", "application/json");

    var slipResponse = await _httpClient.SendAsync(slipRequest);
    var slipContent = await slipResponse.Content.ReadAsStringAsync();

    if (!slipResponse.IsSuccessStatusCode)
        throw new Exception($"Erreur création Salary Slip : {slipResponse.StatusCode} - {slipContent}");

    var slipDoc = JsonSerializer.Deserialize<JsonElement>(slipContent).GetProperty("data");

    // Soumettre le Salary Slip
    var submitSlipRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}api/method/frappe.client.submit")
    {
        Content = new StringContent(JsonSerializer.Serialize(new { doc = slipDoc }), Encoding.UTF8, "application/json")
    };
    submitSlipRequest.Headers.Add("Cookie", $"sid={sid}");
    submitSlipRequest.Headers.Add("Accept", "application/json");

    var submitSlipResponse = await _httpClient.SendAsync(submitSlipRequest);
    var submitSlipContent = await submitSlipResponse.Content.ReadAsStringAsync();

    if (!submitSlipResponse.IsSuccessStatusCode)
        throw new Exception($"Erreur soumission Salary Slip : {submitSlipResponse.StatusCode} - {submitSlipContent}");

    return slipDoc.GetProperty("name").GetString();
}

// Helper
private int DaysInMonth(DateTime date) => DateTime.DaysInMonth(date.Year, date.Month);

    
    // private int MonthDays(DateTime date)
    // {
    //     var nextMonth = new DateTime(date.Year, date.Month, 28).AddDays(4);
    //     return DateTime.DaysInMonth(date.Year, date.Month);
    // }

    private async Task SubmitDocumentAsync(string doctype, string docName)
    {
        var sid = GetSessionId();
        if (sid == null) return;

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}api/method/frappe.client.submit");
        var payload = new
        {
            doc = new
            {
                doctype = doctype,
                name = docName
            }
        };
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        request.Headers.Add("Cookie", $"sid={sid}");
        request.Headers.Add("Accept", "application/json");

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            _logger.LogError("Erreur lors de la soumission du document {Doctype} {DocName} : {StatusCode} - {Content}",
                doctype, docName, response.StatusCode, content);
    }

    public Dictionary<string, object> ConfigureDeductionComponent(Dictionary<string, object> rowData, int dependsOnPaymentDays)
    {
        string formule = rowData.TryGetValue("valeur", out var val) ? val?.ToString() ?? "" : "";
        bool isFormula = !string.IsNullOrEmpty(formule) && formule != "0";

        return new Dictionary<string, object>
        {
            ["salary_component"] = rowData["name"],
            ["amount_based_on_formula"] = isFormula ? 1 : 0,
            ["formula"] = isFormula ? formule : "",
            ["depends_on_payment_days"] = dependsOnPaymentDays
        };
    }

    public Dictionary<string, object> ConfigureEarningComponent(Dictionary<string, object> rowData)
    {
        string formule = rowData.TryGetValue("valeur", out var val) ? val?.ToString() ?? "" : "";
        bool isFormula = !string.IsNullOrEmpty(formule) && formule != "0";

        return new Dictionary<string, object>
        {
            ["salary_component"] = rowData["name"],
            ["amount_based_on_formula"] = isFormula ? 1 : 0,
            ["formula"] = isFormula ? formule : "",
            ["depends_on_payment_days"] = isFormula ? 0 : 1
        };
    }

    public async Task EnsureSalaryComponentAsync(string name, string compType, string abbr)
    {
        compType = compType.Trim();
        if (string.IsNullOrEmpty(compType))
            compType = "Earning"; // Par défaut ou adapte selon besoin

        // Vérifier si le composant existe
        var sid = GetSessionId();
        if (sid == null)
            throw new Exception("Session non valide");

        var existsUrl = $"{_baseUrl}api/resource/Salary Component/{Uri.EscapeDataString(name)}";
        var requestCheck = new HttpRequestMessage(HttpMethod.Get, existsUrl);
        requestCheck.Headers.Add("Cookie", $"sid={sid}");
        requestCheck.Headers.Add("Accept", "application/json");

        var responseCheck = await _httpClient.SendAsync(requestCheck);
        if (responseCheck.IsSuccessStatusCode)
        {
            // Existe déjà, on sort
            return;
        }

        // Créer le composant
        var payload = new Dictionary<string, object>
        {
            ["salary_component"] = name,
            ["type"] = char.ToUpper(compType[0]) + compType.Substring(1).ToLower()
        };

        if (!string.IsNullOrEmpty(abbr))
            payload["salary_component_abbr"] = abbr;

        // Par défaut depends_on_payment_days = 0 si propriété existe
        payload["depends_on_payment_days"] = 0;

        var requestCreate = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}api/resource/Salary Component")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { data = payload }), Encoding.UTF8, "application/json")
        };
        requestCreate.Headers.Add("Cookie", $"sid={sid}");
        requestCreate.Headers.Add("Accept", "application/json");

        var responseCreate = await _httpClient.SendAsync(requestCreate);
        var contentCreate = await responseCreate.Content.ReadAsStringAsync();

        if (!responseCreate.IsSuccessStatusCode)
        {
            throw new Exception($"Erreur création Salary Component : {responseCreate.StatusCode} - {contentCreate}");
        }
    }

    public async Task<string> UpsertSalaryStructureAsync(Dictionary<string, object> structData)
{
    string code = structData["structureCode"].ToString();
    string company = structData.ContainsKey("company") ? structData["company"].ToString() : "";
    string currency = "USD";
    string name = code;

    var sid = GetSessionId();
    if (sid == null)
        throw new Exception("Session invalide");

    // Vérifier si la Salary Structure existe déjà
    var existsUrl = $"{_baseUrl}api/resource/Salary Structure/{Uri.EscapeDataString(name)}";
    var requestCheck = new HttpRequestMessage(HttpMethod.Get, existsUrl);
    requestCheck.Headers.Add("Cookie", $"sid={sid}");
    requestCheck.Headers.Add("Accept", "application/json");

    var responseCheck = await _httpClient.SendAsync(requestCheck);
    if (responseCheck.IsSuccessStatusCode)
    {
        // Structure existante : récupère la version complète
        var contentCheck = await responseCheck.Content.ReadAsStringAsync();

        // Désérialiser le document complet
        var docJson = JsonSerializer.Deserialize<JsonElement>(contentCheck);
        var docData = docJson.GetProperty("data");

        // Soumettre la Salary Structure avec le doc complet
        var submitUrl = $"{_baseUrl}api/method/frappe.client.submit";
        var submitPayload = new { doc = docData };

        var requestSubmit = new HttpRequestMessage(HttpMethod.Post, submitUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(submitPayload), Encoding.UTF8, "application/json")
        };
        requestSubmit.Headers.Add("Cookie", $"sid={sid}");
        requestSubmit.Headers.Add("Accept", "application/json");

        var responseSubmit = await _httpClient.SendAsync(requestSubmit);
        var contentSubmit = await responseSubmit.Content.ReadAsStringAsync();
        if (!responseSubmit.IsSuccessStatusCode)
            throw new Exception($"Erreur soumission Salary Structure : {responseSubmit.StatusCode} - {contentSubmit}");

        return name;
    }

    // Création d’une nouvelle Salary Structure
    var docPayload = new Dictionary<string, object>
    {
        ["name"] = name,
        ["company"] = company,
        ["currency"] = currency,
        ["is_active"] = "Yes",
        ["payroll_frequency"] = "Monthly"
    };

    var earnings = structData.ContainsKey("earnings") ? (List<Dictionary<string, object>>)structData["earnings"] : new List<Dictionary<string, object>>();
    var deductions = structData.ContainsKey("deductions") ? (List<Dictionary<string, object>>)structData["deductions"] : new List<Dictionary<string, object>>();
    var allComponents = earnings.Concat(deductions).ToList();

    foreach (var comp in allComponents)
    {
        string compName = comp["name"].ToString();
        string compType = comp["type"].ToString();
        string abbr = (comp.ContainsKey("abbr") && comp["abbr"] != null)
            ? comp["abbr"].ToString().ToUpper()
            : compName.Substring(0, Math.Min(5, compName.Length)).ToUpper();

        await EnsureSalaryComponentAsync(compName, compType, abbr);
    }

    bool deductionHasFormula = deductions.Any(d => d.ContainsKey("valeur") && d["valeur"] != null && d["valeur"].ToString() != "0");
    int deductionDependsFlag = deductionHasFormula ? 0 : 1;

    var earningsList = earnings.Select(e => ConfigureEarningComponent(e)).ToList();
    var deductionsList = deductions.Select(d => ConfigureDeductionComponent(d, deductionDependsFlag)).ToList();

    docPayload["earnings"] = earningsList;
    docPayload["deductions"] = deductionsList;

    var createUrl = $"{_baseUrl}api/resource/Salary Structure";
    var requestCreate = new HttpRequestMessage(HttpMethod.Post, createUrl)
    {
        Content = new StringContent(JsonSerializer.Serialize(new { data = docPayload }), Encoding.UTF8, "application/json")
    };
    requestCreate.Headers.Add("Cookie", $"sid={sid}");
    requestCreate.Headers.Add("Accept", "application/json");

    var responseCreate = await _httpClient.SendAsync(requestCreate);
    var contentCreate = await responseCreate.Content.ReadAsStringAsync();
    var docJsonAfterCreate = JsonSerializer.Deserialize<JsonElement>(contentCreate);
    var docDataAfterCreate = docJsonAfterCreate.GetProperty("data");
    if (!responseCreate.IsSuccessStatusCode)
            throw new Exception($"Erreur création Salary Structure : {responseCreate.StatusCode} - {contentCreate}");
    // Soumettre la nouvelle Salary Structure
    var submitUrlNew = $"{_baseUrl}api/method/frappe.client.submit";
    var submitPayloadNew = new { doc = docDataAfterCreate };
    var requestSubmitNew = new HttpRequestMessage(HttpMethod.Post, submitUrlNew)
    {
        Content = new StringContent(JsonSerializer.Serialize(submitPayloadNew), Encoding.UTF8, "application/json")
    };
    requestSubmitNew.Headers.Add("Cookie", $"sid={sid}");
    requestSubmitNew.Headers.Add("Accept", "application/json");

    var responseSubmitNew = await _httpClient.SendAsync(requestSubmitNew);
    var contentSubmitNew = await responseSubmitNew.Content.ReadAsStringAsync();
    if (!responseSubmitNew.IsSuccessStatusCode)
        throw new Exception($"Erreur soumission Salary Structure : {responseSubmitNew.StatusCode} - {contentSubmitNew}");

    return name;
}

 
    public async Task<string> UpsertEmployeeAsync(Dictionary<string, object> emp)
    {
        // Assurer que la company existe
        await EnsureCompanyAsync(emp["company"].ToString());

        // Traitement du genre
        string genre = emp.ContainsKey("genre") ? emp["genre"].ToString().Trim().ToLower() : "";
        string gender;
        if (string.IsNullOrEmpty(genre))
        {
            gender = "Other";
            // Afficher un message d'alerte (ici on peut logger ou gérer autrement)
            Console.WriteLine($"Genre non spécifié pour {emp["prenom"]} {emp["nom"]}, utilisation de 'Other'");
        }
        else if (genre == "masculin")
        {
            gender = "Male";
        }
        else if (genre == "feminin" || genre == "féminin")
        {
            gender = "Female";
        }
        else
        {
            gender = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(genre);
        }

        string sid = GetSessionId();
        if (sid == null)
            throw new Exception("Session invalide");

        // Vérifier si l'employé existe déjà via filters
        var queryParams = new Dictionary<string, string>
        {
            ["fields"] = "[\"name\"]",
            ["limit_page_length"] = "1",
            ["filters"] = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["first_name"] = emp["prenom"].ToString(),
                ["last_name"] = emp["nom"].ToString(),
                ["company"] = emp["company"].ToString()
            })
        };
        var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        var url = $"{_baseUrl}api/resource/Employee?{queryString}";

        var requestGet = new HttpRequestMessage(HttpMethod.Get, url);
        requestGet.Headers.Add("Cookie", $"sid={sid}");
        requestGet.Headers.Add("Accept", "application/json");

        var responseGet = await _httpClient.SendAsync(requestGet);
        responseGet.EnsureSuccessStatusCode();
        var contentGet = await responseGet.Content.ReadAsStringAsync();
        using var docGet = JsonDocument.Parse(contentGet);
        var dataElem = docGet.RootElement.GetProperty("data");

        if (dataElem.GetArrayLength() > 0)
        {
            // Employé existe, retourner son name
            return dataElem[0].GetProperty("name").GetString()!;
        }

        // Création d'un nouvel employé
        var newEmployee = new Dictionary<string, object>
        {
            ["first_name"] = emp["prenom"],
            ["last_name"] = emp["nom"],
            ["gender"] = gender,
            ["date_of_joining"] = DateTime.Parse(emp["dateEmbauche"].ToString()).ToString("yyyy-MM-dd"),
            ["date_of_birth"] = DateTime.Parse(emp["dateNaissance"].ToString()).ToString("yyyy-MM-dd"),
            ["company"] = emp["company"]
        };

        var createPayload = new { data = newEmployee };
        var requestPost = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}api/resource/Employee")
        {
            Content = new StringContent(JsonSerializer.Serialize(createPayload), Encoding.UTF8, "application/json")
        };
        requestPost.Headers.Add("Cookie", $"sid={sid}");
        requestPost.Headers.Add("Accept", "application/json");

        var responsePost = await _httpClient.SendAsync(requestPost);
        var contentPost = await responsePost.Content.ReadAsStringAsync();
        if (!responsePost.IsSuccessStatusCode)
            throw new Exception($"Erreur création Employee : {responsePost.StatusCode} - {contentPost}");

        using var docPost = JsonDocument.Parse(contentPost);
        return docPost.RootElement.GetProperty("data").GetProperty("name").GetString()!;
    }


public async Task DeleteAsync(string doctype, string docname)
{
    var id = GetSessionId();
    var baseRequestUrl = $"{_baseUrl}/api/resource/{doctype}/{docname}";

    // Si Salary Structure : supprimer les éléments liés avant
    if (doctype == "Salary Structure")
    {
        // Supprimer les Salary Structure Assignment liés
        var assignments = await GetAssignmentsForStructure(docname);
        foreach (var assignment in assignments)
        {
            await DeleteAsync("Salary Structure Assignment", assignment);
        }
    }

    // Doctypes qui doivent être annulés avant suppression
    var requiresCancel = new[] { "Salary Slip", "Salary Structure" };

    if (requiresCancel.Contains(doctype))
    {
        var cancelRequest = new HttpRequestMessage(HttpMethod.Put, baseRequestUrl);
        cancelRequest.Headers.Add("Cookie", $"sid={id}");
        cancelRequest.Content = new StringContent(
            JsonSerializer.Serialize(new { docstatus = 2 }),
            Encoding.UTF8,
            "application/json"
        );

        var cancelResponse = await _httpClient.SendAsync(cancelRequest);
        if (!cancelResponse.IsSuccessStatusCode)
        {
            var cancelContent = await cancelResponse.Content.ReadAsStringAsync();
            throw new Exception($"Erreur lors de l'annulation de {doctype} '{docname}' : {cancelContent}");
        }
    }

    // Suppression
    var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, baseRequestUrl);
    deleteRequest.Headers.Add("Cookie", $"sid={id}");
    var deleteResponse = await _httpClient.SendAsync(deleteRequest);

    if (!deleteResponse.IsSuccessStatusCode)
    {
        var content = await deleteResponse.Content.ReadAsStringAsync();
        throw new Exception($"Erreur lors de la suppression de {doctype} '{docname}' : {content}");
    }
}


// Méthode pour récupérer les Salary Structure Assignment liés à une Salary Structure
public async Task<List<string>> GetAssignmentsForStructure(string structureName)
{
    var id = GetSessionId();
    var requestUrl = $"{_baseUrl}/api/resource/Salary Structure Assignment?fields=[\"name\",\"docstatus\"]&filters=[[\"salary_structure\",\"=\",\"{structureName}\"]]";
    
    var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
    request.Headers.Add("Cookie", $"sid={id}");

    var response = await _httpClient.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
        throw new Exception($"Erreur lors de la récupération des affectations pour {structureName} : {content}");

    using var jsonDoc = JsonDocument.Parse(content);
    var dataArray = jsonDoc.RootElement.GetProperty("data");

    var names = new List<string>();

    foreach (var element in dataArray.EnumerateArray())
    {
        var name = element.GetProperty("name").GetString();
        var docstatus = element.GetProperty("docstatus").GetInt32();

        if (docstatus == 1) // soumis
        {
            // annuler avant suppression
            var cancelUrl = $"{_baseUrl}/api/resource/Salary Structure Assignment/{name}";
            var cancelRequest = new HttpRequestMessage(HttpMethod.Put, cancelUrl);
            cancelRequest.Headers.Add("Cookie", $"sid={id}");
            cancelRequest.Content = new StringContent(
                JsonSerializer.Serialize(new { docstatus = 2 }),
                Encoding.UTF8,
                "application/json"
            );

            var cancelResponse = await _httpClient.SendAsync(cancelRequest);
            var cancelContent = await cancelResponse.Content.ReadAsStringAsync();
            if (!cancelResponse.IsSuccessStatusCode)
                throw new Exception($"Erreur lors de l'annulation de Salary Structure Assignment '{name}' : {cancelContent}");
        }

        names.Add(name);
    }

    return names;
}


public async Task DeleteAllSalaryComponentsAsync()
{
    var id = GetSessionId();
    var requestUrl = $"{_baseUrl}/api/resource/Salary Component?fields=[\"name\"]";

    var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
    request.Headers.Add("Cookie", $"sid={id}");

    var response = await _httpClient.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
        throw new Exception($"Erreur lors de la récupération des Salary Component : {content}");

    using var jsonDoc = JsonDocument.Parse(content);
    var dataArray = jsonDoc.RootElement.GetProperty("data");

    var components = dataArray.EnumerateArray()
        .Select(e => e.GetProperty("name").GetString())
        .ToList();

    foreach (var component in components)
    {
        try
        {
            await DeleteAsync("Salary Component", component);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("You can disable this Salary Component instead of deleting it"))
            {
                                // Désactiver
                var disableUrl = $"{_baseUrl}/api/resource/Salary Component/{component}";
                var disableRequest = new HttpRequestMessage(HttpMethod.Put, disableUrl);
                disableRequest.Headers.Add("Cookie", $"sid={id}");
                disableRequest.Content = new StringContent(
                    JsonSerializer.Serialize(new { disabled = true }),
                    Encoding.UTF8,
                    "application/json"
                );

                var disableResponse = await _httpClient.SendAsync(disableRequest);
                var disableContent = await disableResponse.Content.ReadAsStringAsync();

                if (!disableResponse.IsSuccessStatusCode)
                    throw new Exception($"Erreur lors de la désactivation du Salary Component '{component}' : {disableContent}");

                // Retenter la suppression après désactivation
                try
                {
                    await DeleteAsync("Salary Component", component);
                }
                catch (Exception innerEx)
                {
                    throw new Exception($"Erreur lors de la suppression après désactivation du Salary Component '{component}' : {innerEx.Message}");
                }
            }
            else
            {
                throw; // Autre erreur
            }
        }
    }
}


    // Appels spécifiques
    public Task DeleteEmployeeAsync(string docName) =>
        DeleteAsync("Employee", docName);

public Task DeleteSalaryStructureAsync(string docName) =>
    DeleteAsync("Salary Structure", docName);

public Task DeleteSalarySlipAsync(string docName) =>
    DeleteAsync("Salary Slip", docName);



    public async Task<ImportResult> ImportBulkDataAsync(BulkImportDto data)
{
    var result = new ImportResult
    {
        Counts = new ImportCounts(),
        Errors = new List<string>()
    };

    var employeesMap = new Dictionary<string, string>();
    var structuresMap = new Dictionary<string, string>();
    var createdEmployees = new List<string>();
    var createdStructures = new List<string>();
    var createdSlips = new List<string>();
    try
    {

        // 1. Import employees
            for (int i = 0; i < data.Employees.Count; i++)
            {
                var emp = data.Employees[i];
                if (string.IsNullOrWhiteSpace(emp.Ref))
                {
                    result.Errors.Add($"Ligne {i + 1} : Identifiant de l’employé manquant.");
                    throw new Exception("Erreur bloquante sur les employés.");
                }

                try
                {
                    var empDict = new Dictionary<string, object>
                    {
                        ["prenom"] = emp.Prenom,
                        ["nom"] = emp.Nom,
                        ["genre"] = emp.Genre,
                        ["dateNaissance"] = emp.DateNaissance,
                        ["dateEmbauche"] = emp.DateEmbauche,
                        ["company"] = emp.Company
                    };

                    var docName = await UpsertEmployeeAsync(empDict);
                    employeesMap[emp.Ref] = docName;
                    createdEmployees.Add(docName);
                    result.Counts.Employees++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Ligne {i + 1} : Erreur employé {emp.Ref} : {ex.Message}");
                    throw new Exception("Erreur bloquante sur les employés.");
                }
            }

        // 2. Group salary structures
        var groupedStructures = new Dictionary<string, StructureGroupedData>();
        foreach (var elem in data.SalaryElements)
        {
            if (string.IsNullOrWhiteSpace(elem.SalaryStructure))
                throw new Exception("Structure de salaire manquante dans un élément.");

            if (!groupedStructures.ContainsKey(elem.SalaryStructure))
            {
                groupedStructures[elem.SalaryStructure] = new StructureGroupedData
                {
                    StructureCode = elem.SalaryStructure,
                    Company = elem.Company,
                    Earnings = new(),
                    Deductions = new()
                };
            }

            if (elem.Type?.ToLower() == "earning")
                groupedStructures[elem.SalaryStructure].Earnings.Add(elem);
            else if (elem.Type?.ToLower() == "deduction")
                groupedStructures[elem.SalaryStructure].Deductions.Add(elem);
            else
                throw new Exception($"Type de composant invalide : {elem.Type}");
        }
        // 3. Import salary structures
        foreach (var kvp in groupedStructures)
        {
            try
            {
                Console.WriteLine("ny isany earning dia" +kvp.Value.Earnings.Count);
                var structDict = new Dictionary<string, object>
                {
                    ["structureCode"] = kvp.Value.StructureCode,
                    ["company"] = kvp.Value.Company,
                    ["earnings"] = kvp.Value.Earnings.Select(e => new Dictionary<string, object>
                    {
                        ["name"] = e.Name,
                        ["type"] = "earning",
                        ["valeur"] = e.Valeur,
                        ["abbr"] = e.Abbr
                    }).ToList(),
                    ["deductions"] = kvp.Value.Deductions.Select(d => new Dictionary<string, object>
                    {
                        ["name"] = d.Name,
                        ["type"] = "deduction",
                        ["valeur"] = d.Valeur,
                        ["abbr"] = d.Abbr
                    }).ToList()
                };
                var structName = await UpsertSalaryStructureAsync(structDict);
                structuresMap[kvp.Key] = structName;
                createdStructures.Add(structName);
                result.Counts.Structures++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Structure '{kvp.Key}' non ajoutée : {ex.Message}");
                throw new Exception("Erreur bloquante sur les structures.");
            }
        }

        // 4. Import salary slips
        for (int i = 0; i < data.SalaryEmps.Count; i++)
        {
            var slip = data.SalaryEmps[i];
            try
            {
                if (!employeesMap.TryGetValue(slip.RefEmploye, out var empName))
                    throw new Exception($"Employé '{slip.RefEmploye}' non trouvé.");

                if (!structuresMap.TryGetValue(slip.Salaire, out var structName))
                    throw new Exception($"Structure '{slip.Salaire}' non trouvée.");

                var slipDict = new Dictionary<string, object>
                {
                    ["mois"] = slip.Mois,
                    ["salaireBase"] = slip.SalaireBase,
                    ["company"] = data.Employees.FirstOrDefault(e => e.Ref == slip.RefEmploye)?.Company ?? ""
                };

                var slipName = await UpsertSalarySlipAsync(slipDict, empName, structName);
                createdSlips.Add(slipName);
                result.Counts.Slips++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Ligne {i + 1} : Erreur bulletin : {ex.Message}");
                throw new Exception("Erreur bloquante sur les bulletins.");
            }
        }

        result.Success = true;
        result.Message = "Import terminé avec succès.";
    }
    catch (Exception ex)
    {
        var rep=await ResetDataAsync();
        result.Success = false;
        result.Message = "Échec de l'import. Annulation des enregistrements créés.";

    // foreach (var slip in createdSlips)
    //     await DeleteSalarySlipAsync(slip);

    // foreach (var structure in createdStructures)
    //     await DeleteSalaryStructureAsync(structure);

    // // Supprimer les Salary Component après toutes les Salary Structure
    // //await DeleteAllSalaryComponentsAsync();

    // foreach (var emp in createdEmployees)
    //     await DeleteEmployeeAsync(emp);
    }
    return result;
}

}