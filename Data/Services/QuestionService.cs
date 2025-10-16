using Core.Entities;
using Core.Enums;
using Core.Interfaces.Services;

namespace Data.Services
{
    public class QuestionService : IQuestionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IQuestionGeneratorService _questionGeneratorService;

        public QuestionService(IUnitOfWork unitofWork, IQuestionGeneratorService questionGeneratorService)
        {
            _unitOfWork = unitofWork;
            _questionGeneratorService = questionGeneratorService;
        }

        public async Task DeleteQuestionAsync(int id)
        {
            var questionToDelete = await _unitOfWork.Questions.GetByIdAsync(id);
            if (questionToDelete == null)
            {
                throw new ArgumentException("A kérdés nem található.", nameof(id));
            }
            _unitOfWork.Questions.Remove(questionToDelete);
            await _unitOfWork.SaveChangesAsync();
        }
        public async Task<bool> GenerateAndSaveQuestionsAsync(int noteId, QuestionType type)
        {
            var note = await _unitOfWork.Notes.GetByIdAsync(noteId);
            if (note == null)
            {
                throw new ArgumentException("A jegyzet nem található.", nameof(noteId));
            }

            var chunks = await _unitOfWork.NoteChunks.GetFilteredAsync(filter: n => n.NoteId == noteId);
            if (!chunks.Any())
            {
                Console.WriteLine($"Nincsenek 'chunks' a {noteId} azonosítójú jegyzethez.");
                return false;
            }

            foreach (var chunk in chunks)
            {
                //var qaPairs = await _questionGeneratorService.GenerateQuestionsAsync(chunk.Content, type, "gemini-2.5-flash");

                var ollamaModelName = "gemma3:12b"; // VAGY a letöltött modell neve (pl. "mistral:latest")
                var qaPairs = await _questionGeneratorService.GenerateQuestionsAsync(chunk.Content, type, ollamaModelName);
                
                if (qaPairs == null || !qaPairs.Any())
                {
                    Console.WriteLine("Az API nem generált kérdéseket.");
                    continue;
                }

                foreach (var pair in qaPairs)
                {
                    List<string> wrongAnswers = new List<string>();
                    if (type == QuestionType.MultipleChoice)
                    {
                        wrongAnswers = await _questionGeneratorService.GenerateWrongAnswersAsync(pair.Answer, chunk.Content, "gemini-2.5-flash");
                        if (wrongAnswers == null || wrongAnswers.Count < 2)
                        {
                            Console.WriteLine("Nincs elég rossz válasz generálva, kihagyjuk ezt a kérdést.");
                            continue;
                        }
                    }
                    else if (type == QuestionType.TrueFalse)
                    {
                        // Ha Igaz/Hamis, hozzáadjuk a hiányzó, ellentétes opciót.
                        // Feltételezzük, hogy a pair.Answer a helyes válasz ('IGAZ' vagy 'HAMIS')
                        if (pair.Answer.Equals("IGAZ", StringComparison.OrdinalIgnoreCase))
                        {
                            wrongAnswers.Add("HAMIS");
                        }
                        else if (pair.Answer.Equals("HAMIS", StringComparison.OrdinalIgnoreCase))
                        {
                            wrongAnswers.Add("IGAZ");
                        }
                    }

                    var question = new Question
                    {
                        TopicId = note.TopicId,
                        Text = pair.Question,
                        QuestionType = type,
                        SourceNoteId = noteId,
                        Answers = new List<Answer>()
                    };

                    // Közös helyes válasz hozzáadása
                    var correctAnswer = new Answer { Text = pair.Answer, IsCorrect = true };
                    if (type == QuestionType.ShortAnswer)
                    {
                        correctAnswer.SampleAnswer = pair.Answer;  // Csak ShortAnswer-nél töltsd ki
                    }
                    question.Answers.Add(correctAnswer);

                    // Hibás válaszok hozzáadása (csak MultipleChoice)
                    if (type == QuestionType.MultipleChoice || type == QuestionType.TrueFalse)
                    {
                        foreach (var wrongAnswerText in wrongAnswers)
                        {
                            var wrongAnswer = new Answer { Text = wrongAnswerText, IsCorrect = false };
                            question.Answers.Add(wrongAnswer);
                        }
                    }

                    await _unitOfWork.Questions.AddAsync(question);
                }
            }
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Question>> GetQuestionsByTopicIdAsync(int topicId)
        {
            if (topicId <= 0)
            {
                throw new ArgumentException("A téma azonosítója érvénytelen.", nameof(topicId));
            }
            return await _unitOfWork.Questions.GetFilteredAsync(q => q.TopicId == topicId);
        }
        public async Task<IEnumerable<Question>> GetQuestionsForNoteAsync(int noteId)
        {
            if (noteId <= 0)
            {
                throw new ArgumentException("A jegyzet azonosítója érvénytelen.", nameof(noteId));
            }

            return await _unitOfWork.Questions.GetFilteredAsync(filter: q => q.SourceNoteId == noteId);
        }
    }
}