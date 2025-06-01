using System;
namespace NewAppErp.Models.Salary;
public class SalarySlip
{
        public string? Name { get; set; }               // ID du bulletin
        public string? Employee { get; set; }           // ID de l'employé
        public string? EmployeeName { get; set; }       // Nom complet
        public DateTime StartDate { get; set; }         // Début de la période de paie
        public DateTime EndDate { get; set; }           // Fin de la période de paie
        public decimal NetPay { get; set; }             // Salaire net
        public string? Status { get; set; } 
}
