using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class Subject
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        [Required]
        public string Title { get; set; }

        public ICollection<Topic> Topics { get; set; }
    }
}
