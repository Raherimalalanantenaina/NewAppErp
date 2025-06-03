namespace NewAppErp.Models.ImportDto
{
    public class SalaryElementImportDto
    {
        public string SalaryStructure { get; set; }
        public string Name { get; set; }
        public string Abbr { get; set; }
        public string Type { get; set; } // earning ou deduction
        public string Valeur { get; set; } // Formule ou montant
        public string Company { get; set; }
    }
}
