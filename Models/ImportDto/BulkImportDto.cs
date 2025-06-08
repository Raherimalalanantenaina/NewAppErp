using System.Collections.Generic;

namespace NewAppErp.Models.ImportDto
{
    public class BulkImportDto
    {
        public List<EmployeeImportDto> Employees { get; set; } = new List<EmployeeImportDto>();
        public List<SalaryElementImportDto> SalaryElements { get; set; } = new List<SalaryElementImportDto>();
        public List<SalaryEmpImportDto> SalaryEmps { get; set; } = new List<SalaryEmpImportDto>();
    }
}
