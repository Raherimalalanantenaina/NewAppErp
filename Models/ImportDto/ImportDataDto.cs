namespace NewAppErp.Models.ImportDto;
public class ImportDataDto
{
    public List<EmployeeImportDto> Employees { get; set; } = new();
    public List<SalaryElementImportDto> SalaryElements { get; set; } = new();
    public List<SalaryEmpImportDto> SalaryEmps { get; set; } = new();
}
