using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class DailyProgress
    {
        public int Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public int TopicId { get; set; }

        // Csak a dátumot tároljuk, időpont nélkül (vagy UTC éjfélt)
        public DateTime Date { get; set; }

        public int QuestionsAnswered { get; set; } = 0;

        // Ez tárolja az aktuális célt, hátha a user megváltoztatja
        public int GoalQuestions { get; set; } = 5;

        // Ezt a service állítja 'true'-ra, amikor eléri a célt
        public bool GoalReached { get; set; } = false;
    }
}