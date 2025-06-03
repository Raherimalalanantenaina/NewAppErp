using System.Collections.Generic;

namespace NewAppErp.Models.ImportDto
{
    public class ImportResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public ImportCountsDto Counts { get; set; }
        public List<ImportErrorDto> Errors { get; set; } = new List<ImportErrorDto>();
    }

    public class ImportCountsDto
    {
        public int Employees { get; set; }
        public int Structures { get; set; }
        public int Slips { get; set; }
    }

    public class ImportErrorDto
    {
        public int? Line { get; set; }
        public string Employee { get; set; }
        public string Error { get; set; }
    }
}