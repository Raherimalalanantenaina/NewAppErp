namespace NewAppErp.Models.ImportDto
{
    public class SalaryEmpImportDto
    {
        public DateTime Mois { get; set; }
        public string RefEmploye { get; set; } = string.Empty;
        public decimal SalaireBase { get; set; }
        public string Salaire { get; set; } = string.Empty;
    }
}
