using Core.Entities;
using Core.Enums;
using Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Data.Context;

namespace Data.Services
{
    public class QuestionService : IQuestionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IQuestionGeneratorService _questionGeneratorService;
        private readonly IAuthService _authService;
        private readonly IDbContextFactory<MemoSphereDbContext> _factory;
        private readonly string _modelName = "gemini-2.5-flash";

        public QuestionService(IUnitOfWork unitofWork, IQuestionGeneratorService questionGeneratorService, IAuthService authService, IDbContextFactory<MemoSphereDbContext> factory)
        {
            _unitOfWork = unitofWork;
            _questionGeneratorService = questionGeneratorService;
            _authService = authService;
            _factory = factory;
        }

        public async Task DeleteQuestionAsync(int id)
        {
            var userId = _authService.GetCurrentUserId();

            var questions = await _unitOfWork.Questions.GetFilteredAsync(
                filter: q => q.Id == id,
                includeProperties: "Topic"
            );
            var questionToDelete = questions.FirstOrDefault();

            if (questionToDelete == null || questionToDelete.Topic.UserId != userId)
            {
                throw new ArgumentException("A kérdés nem található vagy nincs jogosultság a törléséhez.", nameof(id));
            }

            _unitOfWork.Questions.Remove(questionToDelete);
        }

        public async Task<bool> GenerateAndSaveQuestionsAsync(int noteId, QuestionType type)
        {
            var userId = _authService.GetCurrentUserId();

            var note = await _unitOfWork.Notes.GetByIdAsync(noteId);
            if (note == null || note.UserId != userId)
            {
                throw new ArgumentException("A jegyzet nem található vagy nincs jogosultság.", nameof(noteId));
            }

            var chunks = await _unitOfWork.NoteChunks.GetFilteredAsync(filter: n => n.NoteId == noteId);
            if (!chunks.Any())
            {
                Console.WriteLine($"Nincsenek 'chunks' a {noteId} azonosítójú jegyzethez.");
                return false;
            }

            var questionsToAdd = new List<Question>();

            foreach (var chunk in chunks)
            {
                var qaPairs = await _questionGeneratorService.GenerateQuestionsAsync(chunk.Content, type, _modelName);

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
                        wrongAnswers = await _questionGeneratorService.GenerateWrongAnswersAsync(pair.Answer, chunk.Content, _modelName);
                        if (wrongAnswers == null || wrongAnswers.Count < 2)
                        {
                            Console.WriteLine("Nincs elég rossz válasz generálva, kihagyjuk ezt a kérdést.");
                            continue;
                        }
                    }
                    else if (type == QuestionType.TrueFalse)
                    {
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
                        UserId = userId,
                        IsActive = true,
                        Answers = new List<Answer>()
                    };

                    var correctAnswer = new Answer { Text = pair.Answer, IsCorrect = true };
                    if (type == QuestionType.ShortAnswer)
                    {
                        correctAnswer.SampleAnswer = pair.Answer;
                    }
                    question.Answers.Add(correctAnswer);

                    if (type == QuestionType.MultipleChoice || type == QuestionType.TrueFalse)
                    {
                        foreach (var wrongAnswerText in wrongAnswers)
                        {
                            var wrongAnswer = new Answer { Text = wrongAnswerText, IsCorrect = false };
                            question.Answers.Add(wrongAnswer);
                        }
                    }

                    questionsToAdd.Add(question);
                }
            }

            if (!questionsToAdd.Any())
            {
                return false;
            }

            using var context = _factory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                context.Questions.AddRange(questionsToAdd);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                Console.WriteLine($"{questionsToAdd.Count} kérdés sikeresen elmentve (UserId: {userId})");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba a kérdések mentésekor: {ex.Message}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> EvaluateUserShortAnswerAsync(int questionId, string userAnswer)
        {
            var userId = _authService.GetCurrentUserId();

            var questions = await _unitOfWork.Questions.GetFilteredAsync(
                filter: q => q.Id == questionId,
                includeProperties: "Topic,Answers"
            );
            var question = questions.FirstOrDefault();

            if (question == null || question.Topic.UserId != userId)
            {
                throw new ArgumentException("A kérdés nem található vagy nincs jogosultság.", nameof(questionId));
            }

            if (question.QuestionType != QuestionType.ShortAnswer)
            {
                throw new InvalidOperationException("Ez a metódus csak ShortAnswer típusú kérdésekhez használható.");
            }

            var correctAnswer = question.Answers.FirstOrDefault(a => a.IsCorrect);
            if (correctAnswer == null)
            {
                throw new InvalidOperationException("A kérdésnek nincs helyes válasza definiálva.");
            }

            var sourceNote = await _unitOfWork.Notes.GetByIdAsync(question.SourceNoteId ?? 0);
            string context = sourceNote?.Content ?? string.Empty;

            bool isCorrect = await _questionGeneratorService.EvaluateAnswerAsync(
                question.Text,
                userAnswer,
                correctAnswer.SampleAnswer ?? correctAnswer.Text,
                context,
                _modelName
            );

            return isCorrect;
        }

        public async Task<IEnumerable<Question>> GetQuestionsByTopicIdAsync(int topicId)
        {
            var userId = _authService.GetCurrentUserId();

            if (topicId <= 0)
            {
                throw new ArgumentException("A téma azonosítója érvénytelen.", nameof(topicId));
            }
            return await _unitOfWork.Questions.GetFilteredAsync(
                filter: q => q.TopicId == topicId && q.Topic.UserId == userId,
                includeProperties: "Topic"
            );
        }

        public async Task<IEnumerable<Question>> GetQuestionsForNoteAsync(int noteId)
        {
            var userId = _authService.GetCurrentUserId();

            if (noteId <= 0)
            {
                throw new ArgumentException("A jegyzet azonosítója érvénytelen.", nameof(noteId));
            }

            return await _unitOfWork.Questions.GetFilteredAsync(
                filter: q => q.SourceNoteId == noteId && q.SourceNote.UserId == userId,
                includeProperties: "SourceNote"
            );
        }
    }
}