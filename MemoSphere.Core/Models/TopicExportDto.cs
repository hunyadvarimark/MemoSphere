namespace Core.Models
{
    public class TopicExportDto
    {
        public string Title { get; set; } = string.Empty;
        public List<NoteExportDto> Notes { get; set; } = new List<NoteExportDto>();
    }

    public class CreateTopicRequest
    {
        public string Title { get; set; } = string.Empty;
        public int SubjectId { get; set; }
    }

    public class UpdateTopicRequest
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int SubjectId { get; set; }
    }
}