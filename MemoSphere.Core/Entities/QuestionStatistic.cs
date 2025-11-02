using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class QuestionStatistic
    {
        public int Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public int QuestionId { get; set; }
        public Question Question { get; set; }

        public int TimesAsked { get; set; } = 0;
        public int TimesCorrect { get; set; } = 0;
        public int TimesIncorrect { get; set; } = 0;

        public DateTime LastAsked { get; set; } = DateTime.UtcNow;

        // Számított property
        public double SuccessRate => TimesAsked > 0 ? (double)TimesCorrect / TimesAsked : 0.5;
    }
}