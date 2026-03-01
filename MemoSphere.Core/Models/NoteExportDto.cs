using Core.Enums;
using System.Collections.Generic;

namespace Core.Models
{
    public class  NoteExportDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        //A jegyzetekhez tartozo kérdések listája

        public List<QuestionExportDto> Questions { get; set; } = new List<QuestionExportDto>();
    }
}