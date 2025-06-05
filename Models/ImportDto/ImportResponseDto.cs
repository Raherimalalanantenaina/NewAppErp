using System.Collections.Generic;

namespace NewAppErp.Models.ImportDto
{
    public class ImportResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public ImportCountsDto Counts { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class ImportCountsDto
    {
        public int Employees { get; set; }
        public int Structures { get; set; }
        public int Slips { get; set; }
    }
}