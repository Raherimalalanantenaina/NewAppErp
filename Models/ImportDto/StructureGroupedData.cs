using System.Collections.Generic;

namespace NewAppErp.Models.ImportDto
{
    public class StructureGroupedData
    {
        public string StructureCode { get; set; }
        public string Company { get; set; }
        public List<SalaryElementImportDto> Earnings { get; set; }
        public List<SalaryElementImportDto> Deductions { get; set; }
    }
}
