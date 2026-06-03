using System.Collections.Generic;
using Core.Enums;

namespace Core.Models
{
    public class QuestionExportDto
    {
        public string Text { get; set; } = string.Empty;
        public QuestionType QuestionType { get; set; }

        public List<AnswerExportDto> Answers { get; set; } = new List<AnswerExportDto>();
    }

    

    public class CreateQuestionRequest
    {
        public string Text { get; set; } = string.Empty;
        public int QuestionType { get; set; }
        public int TopicId { get; set; }
        public int SourceNoteId { get; set; }
        public List<CreateAnswerEmbeddedRequest> Answers { get; set; } = new List<CreateAnswerEmbeddedRequest>();
    }

    public class UpdateQuestionRequest
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int QuestionType { get; set; }
        public bool IsActive { get; set; }

        public List<UpdateAnswerEmbeddedRequest> Answers { get; set; } = new List<UpdateAnswerEmbeddedRequest>();
    }

    public class CreateAnswerEmbeddedRequest
    {
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
    public class UpdateAnswerEmbeddedRequest
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}