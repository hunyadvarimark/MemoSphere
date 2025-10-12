using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class NoteChunk
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public int NoteId { get; set; }
        public Note Note { get; set; }
    }
}
