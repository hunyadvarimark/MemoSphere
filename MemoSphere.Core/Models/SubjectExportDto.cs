using System.Collections.Generic;

namespace Core.Models
{
    public class SubjectExportDto
    {
        public string Title { get; set; } = string.Empty;
        public List<TopicExportDto> Topics { get; set; } = new List<TopicExportDto>();
    }

    public class CreateSubjectRequest
    {
        public string Title { get; set; } = string.Empty;
    }

    public class UpdateSubjectRequest
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}