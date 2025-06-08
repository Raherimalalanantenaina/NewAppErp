using System;

namespace NewAppErp.Models.ImportDto
{
    public class SalarySlipDto
    {
        public string RefEmploye { get; set; } // Référence de l'employé dans le fichier
        public string Salaire { get; set; } // Code de la structure salariale
        public DateTime Period { get; set; } // Période (ex: 2024-05-01)
        public string Company { get; set; }
    }
}
