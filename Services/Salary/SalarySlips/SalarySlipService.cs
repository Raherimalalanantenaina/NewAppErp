using System.Globalization;
using System.Text.Json;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using NewAppErp.Models.Salary;
using Newtonsoft.Json.Linq;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout.Borders;
namespace NewAppErp.Services.Salary.SalarySlips
{
    public class SalarySlipService : ISalarySlipService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SalarySlipService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["NewAppErp:BaseUrl"];
            _httpContextAccessor = httpContextAccessor;
        }
        private string? GetSessionId()
        {
            return _httpContextAccessor.HttpContext?.Request.Cookies["sid"]
                ?? _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");
        }
        public async Task<List<SalarySlip>> GetSalarySlipsByEmployee(string employeeId, int? month = null, int? year = null)
        {
            var sid = GetSessionId();
            if (string.IsNullOrEmpty(sid))
                throw new UnauthorizedAccessException("Session ID non trouvé.");

            var fields = new[]
            {
                "name", "employee_name", "start_date", "end_date", "net_pay", "status"
            };

            var filters = new List<object[]>
            {
                new object[] { "employee", "=", employeeId }
            };

            var url = $"{_baseUrl}api/resource/Salary Slip?fields={JsonSerializer.Serialize(fields)}&filters={Uri.EscapeDataString(JsonSerializer.Serialize(filters))}&limit_start=0";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", $"sid={sid}");
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var json = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(content);
            var result = new List<SalarySlip>();

            foreach (var item in json?["data"] ?? [])
            {
                var slip = new SalarySlip
                {
                    Name = item.GetValueOrDefault("name")?.ToString(),
                    EmployeeName = item.GetValueOrDefault("employee_name")?.ToString(),
                    StartDate = DateTime.TryParse(item.GetValueOrDefault("start_date")?.ToString(), out var start) ? start : DateTime.MinValue,
                    EndDate = DateTime.TryParse(item.GetValueOrDefault("end_date")?.ToString(), out var end) ? end : DateTime.MinValue,
                    NetPay = decimal.Parse(item.GetValueOrDefault("net_pay")?.ToString() ?? "0", CultureInfo.InvariantCulture)
                };

                result.Add(slip);
            }

            // Filtrage en C#
            if (year.HasValue)
            {
                result = result.Where(s => s.StartDate.HasValue && s.StartDate.Value.Year == year.Value).ToList();
            }
            if (month.HasValue)
            {
                result = result.Where(s => s.StartDate.HasValue && s.StartDate.Value.Month == month.Value).ToList();
            }


            return result.OrderBy(s => s.StartDate).ToList();
        }

        public async Task<SalarySlip> GetSalarySlipDetail(string name)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/resource/Salary Slip/{name}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Erreur lors de la récupération de la fiche de paie {name}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content)["data"];

            var salarySlip = new SalarySlip
            {
                Name = json["name"]?.ToString(),
                Employee = json["employee"]?.ToString(),
                EmployeeName = json["employee_name"]?.ToString(),
                Company = json["company"]?.ToString(),
                Department = json["department"]?.ToString(),
                Designation = json["designation"]?.ToString(),
                SalaryStructure = json["salary_structure"]?.ToString(),
                PostingDate = json["posting_date"]?.ToObject<DateTime?>(),
                StartDate = json["start_date"]?.ToObject<DateTime?>(),
                EndDate = json["end_date"]?.ToObject<DateTime?>(),
                Currency = json["currency"]?.ToString(),
                TotalWorkingDays = decimal.Parse(json["total_working_days"].ToString() ?? "0", CultureInfo.InvariantCulture),
                PaymentDays = decimal.Parse(json["payment_days"].ToString() ?? "0", CultureInfo.InvariantCulture),
                GrossPay = decimal.Parse(json["gross_pay"].ToString() ?? "0", CultureInfo.InvariantCulture),
                BaseGrossPay = decimal.Parse(json["base_gross_pay"].ToString() ?? "0", CultureInfo.InvariantCulture),
                GrossYearToDate = decimal.Parse(json["gross_year_to_date"].ToString() ?? "0", CultureInfo.InvariantCulture),
                TotalDeduction = decimal.Parse(json["total_deduction"].ToString() ?? "0", CultureInfo.InvariantCulture),
                NetPay = decimal.Parse(json["net_pay"].ToString() ?? "0", CultureInfo.InvariantCulture),
                RoundedTotal = decimal.Parse(json["rounded_total"].ToString() ?? "0", CultureInfo.InvariantCulture),
                TotalInWords = json["total_in_words"]?.ToString(),
                Earnings = new List<SalaryComponent>(),
                Deductions = new List<SalaryComponent>()
            };

            // Earnings
            if (json["earnings"] is JArray earningsArray)
            {
                foreach (var e in earningsArray)
                {
                    salarySlip.Earnings.Add(new SalaryComponent
                    {
                        SalaryComponentName = e["salary_component"]?.ToString(),
                        Amount = e["amount"]?.Value<decimal>() ?? 0,
                        YearToDate = e["year_to_date"]?.Value<decimal>() ?? 0,
                        Abbreviation = e["abbr"]?.ToString()
                    });
                }
            }

            // Deductions
            if (json["deductions"] is JArray deductionsArray)
            {
                foreach (var d in deductionsArray)
                {
                    salarySlip.Deductions.Add(new SalaryComponent
                    {
                        SalaryComponentName = d["salary_component"]?.ToString(),
                        Amount = d["amount"]?.Value<decimal>() ?? 0,
                        YearToDate = d["year_to_date"]?.Value<decimal>() ?? 0,
                        Abbreviation = d["abbr"]?.ToString()
                    });
                }
            }

            return salarySlip;
        }
        public byte[] GenerateSalarySlipPdf(SalarySlip slip)
        {
            using var ms = new MemoryStream();
            var writer = new PdfWriter(ms);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, PageSize.A4);

            // Polices et styles
            PdfFont fontNormal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            PdfFont fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont fontItalic = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

            // Styles
            var titleStyle = new Style().SetFont(fontBold).SetFontSize(14);
            var subtitleStyle = new Style().SetFont(fontBold).SetFontSize(11);
            var normalStyle = new Style().SetFont(fontNormal).SetFontSize(10);
            var highlightStyle = new Style().SetFont(fontBold).SetFontSize(11).SetFontColor(DeviceRgb.BLUE);
            var italicStyle = new Style().SetFont(fontItalic).SetFontSize(10);

            // En-tête
            var header = new Paragraph()
                .Add(new Text(slip.Company).AddStyle(titleStyle))
                .Add(new Tab())
                .Add(new Text("FICHE DE PAIE").AddStyle(titleStyle.SetTextAlignment(TextAlignment.RIGHT)))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(15);
            document.Add(header);

            // Ligne séparatrice bleue
            var blueLine = new SolidLine(1f);
            blueLine.SetColor(DeviceRgb.BLUE);
            document.Add(new LineSeparator(blueLine).SetMarginBottom(15));

            // Section employé
            document.Add(new Paragraph()
                .Add(new Text("Informations employé").AddStyle(subtitleStyle))
                .SetMarginBottom(8));

            var employeeInfo = new Paragraph()
                .AddTabStops(new TabStop(150, TabAlignment.LEFT))
                .Add(new Text("Nom:").AddStyle(normalStyle)).Add(new Tab())
                .Add(new Text(slip.EmployeeName).AddStyle(normalStyle)).Add("\n")
                .Add(new Text("Période:").AddStyle(normalStyle)).Add(new Tab())
                .Add(new Text($"{slip.StartDate:dd/MM/yyyy} - {slip.EndDate:dd/MM/yyyy}").AddStyle(normalStyle))
                .SetMarginBottom(15);
            document.Add(employeeInfo);

            // Section Gains
            document.Add(new Paragraph()
                .Add(new Text("Gains").AddStyle(subtitleStyle.SetUnderline()))
                .SetMarginBottom(8));

            foreach (var earning in slip.Earnings)
            {
                document.Add(new Paragraph()
                    .Add(new Text($"• {earning.SalaryComponentName}:").AddStyle(normalStyle))
                    .Add(new Tab())
                    .Add(new Text(earning.Amount.ToString("N2") + " " + slip.Currency).AddStyle(normalStyle))
                    .SetMarginLeft(15));
            }

            document.Add(new Paragraph()
                .Add(new Text("Total Gains:").AddStyle(highlightStyle))
                .Add(new Tab())
                .Add(new Text(slip.GrossPay.ToString("N2") + " " + slip.Currency).AddStyle(highlightStyle))
                .SetMarginBottom(12));

            // Section Déductions
            document.Add(new Paragraph()
                .Add(new Text("Déductions").AddStyle(subtitleStyle.SetUnderline()))
                .SetMarginBottom(8));

            foreach (var deduction in slip.Deductions)
            {
                document.Add(new Paragraph()
                    .Add(new Text($"• {deduction.SalaryComponentName}:").AddStyle(normalStyle))
                    .Add(new Tab())
                    .Add(new Text(deduction.Amount.ToString("N2") + " " + slip.Currency).AddStyle(normalStyle))
                    .SetMarginLeft(15));
            }

            document.Add(new Paragraph()
                .Add(new Text("Total Déductions:").AddStyle(highlightStyle))
                .Add(new Tab())
                .Add(new Text(slip.TotalDeduction.ToString("N2") + " " + slip.Currency).AddStyle(highlightStyle))
                .SetMarginBottom(15));

            // Section Récapitulative avec encadré
            var summary = new Div()
                .SetBorder(new SolidBorder(1))
                .SetPadding(10)
                .SetMarginBottom(15);

            summary.Add(new Paragraph()
                .Add(new Text("Récapitulatif").AddStyle(subtitleStyle.SetUnderline()))
                .SetMarginBottom(8));

            summary.Add(new Paragraph()
                .Add(new Text("Net à payer:").AddStyle(highlightStyle))
                .Add(new Tab())
                .Add(new Text(slip.NetPay.ToString("N2") + " " + slip.Currency).AddStyle(highlightStyle)));

            summary.Add(new Paragraph()
                .Add(new Text("Montant en lettres:").AddStyle(normalStyle))
                .Add(new Tab())
                .Add(new Text(slip.TotalInWords).AddStyle(italicStyle)));

            document.Add(summary);

            // Pied de page
            var grayLine = new SolidLine(0.5f);
            document.Add(new LineSeparator(grayLine));
            document.Add(new Paragraph()
                .Add(new Text($"Document généré le {DateTime.Now:dd/MM/yyyy}"))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(8)
                .SetMarginTop(5));

            document.Close();
            return ms.ToArray();
        }

        public async Task<List<SalarySlip>> GetSalarySlipsAsync(int? month, int? year)
        {
            var sid = GetSessionId();
            if (string.IsNullOrEmpty(sid))
                throw new UnauthorizedAccessException("Session ID non trouvé.");

            var fields = new[] { "name", "employee", "employee_name", "department", "designation", "net_pay", "start_date", "gross_pay", "total_deduction" };
            var url = $"{_baseUrl}api/resource/Salary Slip?fields={JsonSerializer.Serialize(fields)}&limit_start=0&limit_page_length=0&order_by=name%20asc";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", $"sid={sid}");
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content)["data"] as JArray;

            var slips = new List<SalarySlip>();

            foreach (var item in json ?? new JArray())
            {
                var slip = new SalarySlip
                {
                    Name = item["name"]?.ToString(),
                    Employee = item["employee"]?.ToString(),
                    EmployeeName = item["employee_name"]?.ToString(),
                    Department = item["department"]?.ToString(),
                    Designation = item["designation"]?.ToString(),
                    StartDate = item["start_date"]?.ToObject<DateTime?>(),
                    GrossPay = item["gross_pay"]?.Value<decimal>() ?? 0,
                    TotalDeduction = item["total_deduction"]?.Value<decimal>() ?? 0,
                    NetPay = item["net_pay"]?.Value<decimal>() ?? 0,
                    Earnings = new List<SalaryComponent>(),
                    Deductions = new List<SalaryComponent>()
                };

                // Appel pour charger earnings et deductions
                var detailRequest = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}api/resource/Salary Slip/{slip.Name}");
                detailRequest.Headers.Add("Cookie", $"sid={sid}");
                var slipDetailResp = await _httpClient.SendAsync(detailRequest);
                slipDetailResp.EnsureSuccessStatusCode();

                var slipDetailJson = JObject.Parse(await slipDetailResp.Content.ReadAsStringAsync());
                var earnings = slipDetailJson["data"]?["earnings"] as JArray;
                var deductions = slipDetailJson["data"]?["deductions"] as JArray;

                if (earnings != null)
                {
                    foreach (var e in earnings)
                    {
                        slip.Earnings.Add(new SalaryComponent
                        {
                            SalaryComponentName = e["salary_component"]?.ToString(),
                            Amount = e["amount"]?.Value<decimal>() ?? 0,
                            Abbreviation = e["abbr"]?.ToString(),
                            YearToDate = e["year_to_date"]?.Value<decimal>() ?? 0
                        });
                    }
                }

                if (deductions != null)
                {
                    foreach (var d in deductions)
                    {
                        slip.Deductions.Add(new SalaryComponent
                        {
                            SalaryComponentName = d["salary_component"]?.ToString(),
                            Amount = d["amount"]?.Value<decimal>() ?? 0,
                            Abbreviation = d["abbr"]?.ToString(),
                            YearToDate = d["year_to_date"]?.Value<decimal>() ?? 0
                        });
                    }
                }

                slips.Add(slip);
            }

            // 👉 Filtrage en C# ici
            if (year.HasValue)
            {
                slips = slips.Where(s => s.StartDate.HasValue && s.StartDate.Value.Year == year.Value).ToList();
            }

            if (month.HasValue)
            {
                slips = slips.Where(s => s.StartDate.HasValue && s.StartDate.Value.Month == month.Value).ToList();
            }

            return slips;
        }

        public async Task<List<MonthlySalaryComponentTotals>> GetMonthlySalaryComponentTotalsAsync(List<SalarySlip> slips, List<string> componentNames)
        {
            var result = new Dictionary<string, MonthlySalaryComponentTotals>();

            foreach (var slip in slips)
            {
                if (slip.StartDate == null) continue;

                var monthKey = slip.StartDate.Value.ToString("yyyy-MM");

                if (!result.TryGetValue(monthKey, out var monthTotals))
                {
                    monthTotals = new MonthlySalaryComponentTotals
                    {
                        Month = monthKey,
                        Components = componentNames.ToDictionary(name => name, name => 0m)
                    };
                    result[monthKey] = monthTotals;
                }

                monthTotals.NetPay += slip.NetPay;
                monthTotals.GrossPay += slip.GrossPay;
                monthTotals.TotalDeduction += slip.TotalDeduction;

                foreach (var earning in slip.Earnings)
                {
                    if (!monthTotals.Components.ContainsKey(earning.SalaryComponentName))
                        monthTotals.Components[earning.SalaryComponentName] = 0;
                    monthTotals.Components[earning.SalaryComponentName] += earning.Amount;
                }

                foreach (var deduction in slip.Deductions)
                {
                    if (!monthTotals.Components.ContainsKey(deduction.SalaryComponentName))
                        monthTotals.Components[deduction.SalaryComponentName] = 0;
                    monthTotals.Components[deduction.SalaryComponentName] += deduction.Amount;
                }
            }

            return result.Values.OrderBy(r => r.Month).ToList();
        }


        public async Task<List<EmployeeSalaryComponentGridViewModel>> BuildEmployeeSalaryViewModelsAsync(List<SalarySlip> slips, List<string> componentNames)
        {
            var viewModels = new List<EmployeeSalaryComponentGridViewModel>();

            foreach (var slip in slips)
            {

                var model = new EmployeeSalaryComponentGridViewModel
                {
                    EmployeeName = slip.EmployeeName,
                    Department = slip.Department,
                    Designation = slip.Designation,
                    NetPay = slip.NetPay,
                    GrossPay = slip.GrossPay,
                    TotalDeduction = slip.TotalDeduction,
                    Components = componentNames.ToDictionary(name => name, name => 0m),
                    StartDate = slip.StartDate
                };

                // Ajoute les valeurs des earnings
                foreach (var earning in slip.Earnings)
                {
                    if (model.Components.ContainsKey(earning.SalaryComponentName))
                        model.Components[earning.SalaryComponentName] = earning.Amount;
                }

                // Ajoute les valeurs des deductions
                foreach (var deduction in slip.Deductions)
                {
                    if (model.Components.ContainsKey(deduction.SalaryComponentName))
                        model.Components[deduction.SalaryComponentName] = deduction.Amount;
                }

                viewModels.Add(model);
            }

            return viewModels;
        }

       public async Task<List<MonthlySalaryChartData>> GetSalaryChartDataAsync(List<SalarySlip> slips, List<string> componentNames)
        {
            var result = new List<MonthlySalaryChartData>();

            var grouped = slips
                .Where(s => s.StartDate.HasValue) // On filtre ceux qui ont une date
                .GroupBy(s => s.StartDate.Value.ToString("yyyy-MM"))
                .OrderBy(g => g.Key);


            foreach (var group in grouped)
            {
                var data = new MonthlySalaryChartData
                {
                    Month = group.Key,
                    NetPay = group.Sum(s => s.NetPay),
                    Components = componentNames.ToDictionary(name => name, name => 0m) // Initialise toutes les composantes à 0
                };

                foreach (var slip in group)
                {
                    foreach (var earning in slip.Earnings)
                    {
                        if (data.Components.ContainsKey(earning.SalaryComponentName))
                            data.Components[earning.SalaryComponentName] += earning.Amount;
                    }

                    foreach (var deduction in slip.Deductions)
                    {
                        if (data.Components.ContainsKey(deduction.SalaryComponentName))
                            data.Components[deduction.SalaryComponentName] += deduction.Amount;
                    }
                }

                result.Add(data);
            }

            return result;
        }

        public async Task<List<EmployeeSalaryComponentGridViewModel>> BuildEmployeeSalaryViewModelsMois(List<SalarySlip> slips, List<string> componentNames)
        {
            var grouped = slips
                .Where(s => s.StartDate.HasValue)
                .GroupBy(s => new
                {
                    s.EmployeeName,
                    MonthName = s.StartDate.Value.ToString("MMMM", new System.Globalization.CultureInfo("fr-FR")) // ex: "mars"
                })
                .OrderBy(g => g.Key.EmployeeName)
                .ThenBy(g => DateTime.ParseExact(g.Key.MonthName, "MMMM", new System.Globalization.CultureInfo("fr-FR")));

            var viewModels = new List<EmployeeSalaryComponentGridViewModel>();

            foreach (var group in grouped)
            {
                var model = new EmployeeSalaryComponentGridViewModel
                {
                    EmployeeName = group.Key.EmployeeName,
                    Department = group.First().Department,
                    Designation = group.First().Designation,
                    NetPay = group.Sum(s => s.NetPay),
                    GrossPay = group.Sum(s => s.GrossPay),
                    TotalDeduction = group.Sum(s => s.TotalDeduction),
                    Components = componentNames.ToDictionary(name => name, name => 0m),
                    StartDate = group.Min(s => s.StartDate)
                };

                foreach (var slip in group)
                {
                    foreach (var earning in slip.Earnings)
                    {
                        if (model.Components.ContainsKey(earning.SalaryComponentName))
                            model.Components[earning.SalaryComponentName] += earning.Amount;
                    }

                    foreach (var deduction in slip.Deductions)
                    {
                        if (model.Components.ContainsKey(deduction.SalaryComponentName))
                            model.Components[deduction.SalaryComponentName] += deduction.Amount;
                    }
                }

                viewModels.Add(model);
            }

            return viewModels;
        }

    }
}