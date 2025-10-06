using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.Entities
{
    public class Question
    {
        public  int Id { get; set; }

        [Required]
        public string Text { get; set; }

        public QuestionType QuestionType { get; set; }
        public int TopicId { get; set; }
        public Topic Topic { get; set; }

        public int? SourceNoteId { get; set; }
        public Note SourceNote { get; set; }

        public ICollection<Answer> Answers { get; set; }

    }
}
