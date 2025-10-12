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
                // **1. Hiba javítása: Átadjuk a típust!**
                var qaPairs = await _questionGeneratorService.GenerateQuestionsAsync(
                    chunk.Content,
                    type, // <<== Ezt adjuk át!
                    "gemini-2.5-flash");

                if (qaPairs == null || !qaPairs.Any())
                {
                    Console.WriteLine("Az API nem generált kérdéseket.");
                    continue;
                }

                foreach (var pair in qaPairs)
                {
                    // **2. Csak a Feleletválasztós típushoz generálunk hibás válaszokat!**
                    List<string> wrongAnswers = new List<string>();
                    if (type == QuestionType.MultipleChoice)
                    {
                        wrongAnswers = await _questionGeneratorService.GenerateWrongAnswersAsync(
                           pair.Answer,
                           chunk.Content,
                           "gemini-2.5-flash");

                        if (wrongAnswers == null || !wrongAnswers.Any())
                        {
                            Console.WriteLine("Az API nem generált rossz válaszokat.");
                            // Feleletválasztós kvíz esetében kihagyhatjuk, ha nincs 3 rossz válasz.
                            continue;
                        }
                    }
                    // Kifejtős és Eldöntendő típusoknál a 'wrongAnswers' üres marad.

                    var question = new Question
                    {
                        TopicId = note.TopicId,
                        Text = pair.Question,
                        QuestionType = type, // <<== Beállítjuk a menteni kívánt típust!
                        SourceNoteId = noteId,
                        Answers = new List<Answer>()
                    };

                    // **3. Kifejtős kérdések kezelése (csak a helyes válasz)**
                    if (type == QuestionType.ShortAnswer || type == QuestionType.TrueFalse)
                    {
                        // ShortAnswer és True/False típusoknál csak a helyes válasz kell, 
                        // nincs szükség a többi Answer Entitásra a feleletválasztós logika szerint.
                        // De a mentéshez eltároljuk a helyes választ a DB Answer táblájában.
                        var correctAnswer = new Answer
                        {
                            Text = pair.Answer,
                            IsCorrect = true
                        };
                        question.Answers.Add(correctAnswer);
                    }

                    // **4. Feleletválasztós kérdések kezelése (helyes + rossz válaszok)**
                    else if (type == QuestionType.MultipleChoice)
                    {
                        var correctAnswer = new Answer
                        {
                            Text = pair.Answer,
                            IsCorrect = true
                        };
                        question.Answers.Add(correctAnswer);

                        foreach (var wrongAnswerText in wrongAnswers)
                        {
                            var wrongAnswer = new Answer
                            {
                                Text = wrongAnswerText,
                                IsCorrect = false
                            };
                            question.Answers.Add(wrongAnswer);
                        }
                    }

                    await _unitOfWork.Questions.AddAsync(question);
                }
            }
            // Az összes változás mentése a végén
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Question>> GetQuestionsByTopicIdAsync(int topicId)
        {
            if (topicId <= 0)
            {
                throw new ArgumentException("A téma azonosítója érvénytelen.", nameof(topicId));
            }
            return await _unitOfWork.Questions.GetFilteredAsync(n => n.TopicId == topicId);
        }
    }
}