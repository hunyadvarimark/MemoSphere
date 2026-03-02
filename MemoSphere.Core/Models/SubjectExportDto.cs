namespace Core.Models
{
    public class SubjectExportDto
    {
        public string Title { get; set; } = string.Empty;
        public List<TopicExportDto> Topics { get; set; } = new List<TopicExportDto>();
    }
}