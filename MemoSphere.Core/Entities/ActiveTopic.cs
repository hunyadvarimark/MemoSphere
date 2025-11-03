using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class ActiveTopic
    {
        public int Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public int TopicId { get; set; }
        public Topic Topic { get; set; }

        // Beállítások
        public int DailyGoalQuestions { get; set; } = 5;

        // Időbélyegek
        public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastPracticedAt { get; set; } 

        // Streakek (Eredmény)
        public int CurrentStreak { get; set; } = 0;
        public int LongestStreak { get; set; } = 0;

        // Státusz
        public bool IsActive { get; set; } = true;
    }
}