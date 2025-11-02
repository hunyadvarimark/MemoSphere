using Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities
{
    public class Question
    {
        public  int Id { get; set; }
        public Guid UserId { get; set; }

        [Required]
        public string Text { get; set; }

        public QuestionType QuestionType { get; set; }
        public int TopicId { get; set; }
        public Topic Topic { get; set; }

        public int? SourceNoteId { get; set; }
        public Note SourceNote { get; set; }

        public ICollection<Answer> Answers { get; set; }
        public bool IsActive { get; set; } = true;

        [NotMapped]
        public double? DebugWeight { get; set; }

        [NotMapped]
        public QuestionStatistic DebugStatistic { get; set; }

    }
}
