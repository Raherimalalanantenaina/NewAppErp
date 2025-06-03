using System.Collections.Generic;

namespace NewAppErp.Models.ImportDto
{
    public class ImportResult
    {
        public List<EmployeeImportDto> ValidRows { get; set; } = new List<EmployeeImportDto>();
        public List<SalaryElementImportDto> SalaryElements { get; set; } = new List<SalaryElementImportDto>();
        public List<SalaryEmpImportDto> SalaryEmpList { get; set; } = new List<SalaryEmpImportDto>();
        public List<string> Errors { get; set; } = new List<string>();
        public ImportResponseDto ImportSummary { get; set; }
        public string ApiMessage { get; set; }

        public bool HasErrors => Errors.Count > 0;

        public void MergeResults(params ImportResult[] results)
        {
            foreach (var result in results)
            {
                ValidRows.AddRange(result.ValidRows);
                SalaryElements.AddRange(result.SalaryElements);
                SalaryEmpList.AddRange(result.SalaryEmpList);
                Errors.AddRange(result.Errors);
            }
        }

        public void ProcessApiResponse(ImportResponseDto response)
        {
            ImportSummary = response;
            ApiMessage = response.Message;

            if (response.Errors != null)
            {
                foreach (var error in response.Errors)
                {
                    Errors.Add($"Line {error.Line} - Employee {error.Employee}: {error.Error}");
                }
            }
        }
    }

    public class FrappeApiResult<T>
    {
        public T Message { get; set; }
        public object Exc { get; set; }
    }
}