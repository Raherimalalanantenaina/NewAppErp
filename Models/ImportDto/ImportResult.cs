using System.Collections.Generic;

namespace NewAppErp.Models.ImportDto
{
    public class ImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public ImportCounts Counts { get; set; } = new ImportCounts();
        public List<string> Errors { get; set; } = new List<string>();

        public List<EmployeeImportDto> employeeImportDtos { get; set; } = new List<EmployeeImportDto>();
        public List<SalaryElementImportDto> salaireElements { get; set; } = new List<SalaryElementImportDto>();
        public List<SalaryEmpImportDto> salaryEmp { get; set; } = new List<SalaryEmpImportDto>();
    }

    public class ImportCounts
    {
        public int Employees { get; set; }
        public int Structures { get; set; }
        public int Slips { get; set; }
    }
}
