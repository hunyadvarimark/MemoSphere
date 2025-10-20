using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class Topic
    {
    
        public int Id { get; set; }
        public Guid UserId { get; set; }

        [Required]
        public string Title { get; set; }

        public int SubjectId { get; set; }
        public Subject Subject { get; set; }

        public ICollection<Note> Notes { get; set; }
        public ICollection<Question> Questions { get; set; }

    }
}
