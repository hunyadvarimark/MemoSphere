namespace Core.Models
{
    public class TopicExportDto
    {
        public string Title { get; set; } = string.Empty;
        public List<NoteExportDto> Notes { get; set; } = new List<NoteExportDto>();
    }
}
