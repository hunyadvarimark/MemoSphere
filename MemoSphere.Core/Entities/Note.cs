using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class Note
    {
    
        public int Id { get; set; }
        
        [Required]
        public string Content { get; set; }
        [Required]
        public string Title { get; set; }
        public int TopicId { get; set; }
        public Topic Topic { get; set; }
    }
}
