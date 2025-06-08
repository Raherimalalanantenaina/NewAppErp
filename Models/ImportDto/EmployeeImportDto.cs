using System;

namespace NewAppErp.Models.ImportDto
{
    public class EmployeeImportDto
    {
        public string Ref { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Genre { get; set; }
        public DateTime DateNaissance { get; set; }
        public DateTime DateEmbauche { get; set; }
        public string Company { get; set; }
    }
}
