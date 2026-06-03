using System.Collections.Generic;

namespace Core.Models
{
    public class NoteExportDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<QuestionExportDto> Questions { get; set; } = new List<QuestionExportDto>();
    }

    public class CreateNoteRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int TopicId { get; set; }
    }

    public class UpdateNoteRequest
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int TopicId { get; set; }
    }
}