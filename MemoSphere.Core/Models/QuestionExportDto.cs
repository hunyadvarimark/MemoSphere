using Core.Enums;
using System.Collections.Generic;


public class QuestionExportDto
{
    public string Text { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }

    // A kérdéshez tartozó válaszok listája
    public List<AnswerExportDto> Answers { get; set; } = new();
}
