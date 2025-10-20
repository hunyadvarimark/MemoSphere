using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class Answer
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        [Required]
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
        public int QuestionId { get; set; }
        public Question Question { get; set; }
        public string? SampleAnswer { get; set; }
    }
}
