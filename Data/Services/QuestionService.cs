using Core.Entities;
using Core.Enums;
using Core.Interfaces.Services;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Data.Services
{
    public class QuestionService : IQuestionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IQuestionGeneratorService _questionGeneratorService;
        private readonly IAuthService _authService;
        private readonly IDbContextFactory<MemoSphereDbContext> _factory;
        private readonly IActiveLearningService _activeLearningService;
        private readonly string _modelName = "gemini-2.5-flash";
        private const int MaxChunkSizeForBatch = 3000; // karakterben
        private const int MaxParallelTasks = 3;

        public QuestionService(IUnitOfWork unitofWork,
            IQuestionGeneratorService questionGeneratorService,
            IAuthService authService,
            IDbContextFactory<MemoSphereDbContext> factory,
            IActiveLearningService activeLearningService)
        {
            _unitOfWork = unitofWork;
            _questionGeneratorService = questionGeneratorService;
            _authService = authService;
            _factory = factory;
            _activeLearningService = activeLearningService;

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
            var executionId = Guid.NewGuid().ToString().Substring(0, 8);
            Console.WriteLine($"╔═══════════════════════════════════════════════════════════");
            Console.WriteLine($"║ FUTÁS KEZDETE - ID: {executionId}");
            Console.WriteLine($"║ NoteId: {noteId}, Type: {type}");
            Console.WriteLine($"║ Időpont: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            Console.WriteLine($"╚═══════════════════════════════════════════════════════════");

            var userId = _authService.GetCurrentUserId();

            var note = await _unitOfWork.Notes.GetByIdAsync(noteId);
            if (note == null || note.UserId != userId)
            {
                throw new ArgumentException("A jegyzet nem található vagy nincs jogosultság.", nameof(noteId));
            }

            var chunks = (await _unitOfWork.NoteChunks.GetFilteredAsync(filter: n => n.NoteId == noteId)).ToList();
            if (!chunks.Any())
            {
                Console.WriteLine($"Nincsenek 'chunks' a {noteId} azonosítójú jegyzethez.");
                return false;
            }

            // DEBUG: Chunk információk kiírása
            Console.WriteLine($"=== DEBUG INFO ===");
            Console.WriteLine($"Jegyzet hossza: {note.Content?.Length ?? 0} karakter");
            Console.WriteLine($"Chunk-ok száma: {chunks.Count}");
            foreach (var chunk in chunks)
            {
                Console.WriteLine($"  - Chunk {chunk.Id}: {chunk.Content?.Length ?? 0} karakter");
            }

            var questionsToAdd = new List<Question>();

            // Optimalizálás: kis chunk-okat egyesítjük, nagyokat párhuzamosan dolgozzuk fel
            var chunksToProcess = OptimizeChunks(chunks);

            Console.WriteLine($"Batch-ek száma az optimalizálás után: {chunksToProcess.Count}");
            for (int i = 0; i < chunksToProcess.Count; i++)
            {
                var totalChars = chunksToProcess[i].Sum(c => c.Content?.Length ?? 0);
                Console.WriteLine($"  - Batch {i + 1}: {chunksToProcess[i].Count} chunk, összesen {totalChars} karakter");
            }

            // Párhuzamos feldolgozás limittel
            var semaphore = new SemaphoreSlim(MaxParallelTasks);
            var tasks = chunksToProcess.Select(async chunkGroup =>
            {
                await semaphore.WaitAsync();
                try
                {
                    string combinedContent = string.Join("\n\n", chunkGroup.Select(c => c.Content));
                    return await ProcessChunkAsync(combinedContent, note.TopicId, noteId, userId, type);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);

            foreach (var questions in results)
            {
                Console.WriteLine($"Egy batch {questions.Count} kérdést generált");
                questionsToAdd.AddRange(questions);
            }

            Console.WriteLine($"=== ÖSSZESEN: {questionsToAdd.Count} kérdés generálódott ===");
            Console.WriteLine($"╔═══════════════════════════════════════════════════════════");
            Console.WriteLine($"║ FUTÁS VÉGE - ID: {executionId}");
            Console.WriteLine($"║ Generált kérdések: {questionsToAdd.Count}");
            Console.WriteLine($"║ Időpont: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            Console.WriteLine($"╚═══════════════════════════════════════════════════════════");

            if (!questionsToAdd.Any())
            {
                Console.WriteLine("Nem sikerült kérdéseket generálni.");
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

        private List<List<NoteChunk>> OptimizeChunks(List<NoteChunk> chunks)
        {
            var result = new List<List<NoteChunk>>();
            var currentBatch = new List<NoteChunk>();
            int currentBatchSize = 0;

            foreach (var chunk in chunks)
            {
                int chunkSize = chunk.Content?.Length ?? 0;

                // Ha a chunk önmagában túl nagy, külön dolgozzuk fel
                if (chunkSize > MaxChunkSizeForBatch)
                {
                    if (currentBatch.Any())
                    {
                        result.Add(new List<NoteChunk>(currentBatch));
                        currentBatch.Clear();
                        currentBatchSize = 0;
                    }
                    result.Add(new List<NoteChunk> { chunk });
                }
                // Ha hozzáadva túllépné a limitet, új batch-et kezdünk
                else if (currentBatchSize + chunkSize > MaxChunkSizeForBatch && currentBatch.Any())
                {
                    result.Add(new List<NoteChunk>(currentBatch));
                    currentBatch.Clear();
                    currentBatchSize = 0;
                    currentBatch.Add(chunk);
                    currentBatchSize += chunkSize;
                }
                // Egyébként hozzáadjuk a jelenlegi batch-hez
                else
                {
                    currentBatch.Add(chunk);
                    currentBatchSize += chunkSize;
                }
            }

            if (currentBatch.Any())
            {
                result.Add(currentBatch);
            }

            return result;
        }

        private async Task<List<Question>> ProcessChunkAsync(string content, int topicId, int noteId, Guid userId, QuestionType type)
        {
            var questions = new List<Question>();

            try
            {
                var qaPairs = await _questionGeneratorService.GenerateQuestionsAsync(content, type, _modelName);

                if (qaPairs == null || !qaPairs.Any())
                {
                    Console.WriteLine("Az API nem generált kérdéseket ehhez a chunk-hoz.");
                    return questions;
                }

                foreach (var pair in qaPairs)
                {
                    // Validáció
                    if (type == QuestionType.MultipleChoice && (pair.WrongAnswers == null || pair.WrongAnswers.Count < 2))
                    {
                        Console.WriteLine("Nincs elég rossz válasz, kihagyjuk ezt a kérdést.");
                        continue;
                    }

                    var question = new Question
                    {
                        TopicId = topicId,
                        Text = pair.Question,
                        QuestionType = type,
                        SourceNoteId = noteId,
                        UserId = userId,
                        IsActive = true,
                        Answers = new List<Answer>()
                    };

                    // Helyes válasz hozzáadása
                    var correctAnswer = new Answer { Text = pair.Answer, IsCorrect = true };
                    if (type == QuestionType.ShortAnswer)
                    {
                        correctAnswer.SampleAnswer = pair.Answer;
                    }
                    question.Answers.Add(correctAnswer);

                    // Rossz válaszok hozzáadása (már a generálás során kaptuk)
                    if (type == QuestionType.MultipleChoice || type == QuestionType.TrueFalse)
                    {
                        foreach (var wrongAnswerText in pair.WrongAnswers)
                        {
                            var wrongAnswer = new Answer { Text = wrongAnswerText, IsCorrect = false };
                            question.Answers.Add(wrongAnswer);
                        }
                    }

                    questions.Add(question);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba a chunk feldolgozása során: {ex.Message}");
            }

            return questions;
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

        // STATISZTIKA RÖGZÍTÉSE
        public async Task RecordAnswerAsync(int questionId, bool isCorrect)
        {
            var userId = _authService.GetCurrentUserId();

            using var context = _factory.CreateDbContext();

            // Keresünk létező statisztikát
            var statistic = await context.QuestionStatistics
                .FirstOrDefaultAsync(qs => qs.UserId == userId && qs.QuestionId == questionId);

            if (statistic == null)
            {
                // Ha még nincs, létrehozzuk
                statistic = new QuestionStatistic
                {
                    UserId = userId,
                    QuestionId = questionId,
                    TimesAsked = 0,
                    TimesCorrect = 0,
                    TimesIncorrect = 0
                };
                context.QuestionStatistics.Add(statistic);
            }

            // Frissítjük a statisztikát
            statistic.TimesAsked++;
            if (isCorrect)
            {
                statistic.TimesCorrect++;
            }
            else
            {
                statistic.TimesIncorrect++;
            }
            statistic.LastAsked = DateTime.UtcNow;

            await context.SaveChangesAsync();

            var question = await context.Questions
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question != null)
            {
                await _activeLearningService.UpdateProgressAsync(question.TopicId, isCorrect);
            }
            else
            {
                Debug.WriteLine($"[RecordAnswerAsync] Hiba: A {questionId} ID-jű kérdés nem található a TopicId lekéréséhez.");
            }
        }

        // SÚLYOZOTT KÉRDÉSEK LEKÉRÉSE
        public async Task<List<Question>> GetWeightedQuestionsAsync(int topicId, int count, QuestionType? type = null)
        {
            var userId = _authService.GetCurrentUserId();

            using var context = _factory.CreateDbContext();

            var questionsQuery = context.Questions
                .Include(q => q.Answers)
                .Include(q => q.Topic)
                .Where(q => q.TopicId == topicId && q.Topic.UserId == userId && q.IsActive);

            if (type.HasValue)
            {
                questionsQuery = questionsQuery.Where(q => q.QuestionType == type.Value);
            }

            var allQuestions = await questionsQuery.ToListAsync();

            if (!allQuestions.Any())
            {
                return new List<Question>();
            }

            var questionIds = allQuestions.Select(q => q.Id).ToList();
            var statistics = await context.QuestionStatistics
                .Where(qs => qs.UserId == userId && questionIds.Contains(qs.QuestionId))
                .ToDictionaryAsync(qs => qs.QuestionId);

            // ✅ DEBUG LOG: Kérdések súlyai
            Console.WriteLine($"╔═══════════════════════════════════════════════════════════");
            Console.WriteLine($"║ WEIGHTED QUESTION SELECTION - TopicId: {topicId}");
            Console.WriteLine($"║ Total questions: {allQuestions.Count}, Requested: {count}");
            Console.WriteLine($"╚═══════════════════════════════════════════════════════════");

            var weightedQuestions = allQuestions.Select(q =>
            {
                double weight = CalculateWeight(q.Id, statistics);

                // ✅ DEBUG: Súly információ hozzáadása a kérdéshez
                q.DebugWeight = weight;
                q.DebugStatistic = statistics.ContainsKey(q.Id) ? statistics[q.Id] : null;

                // ✅ DEBUG LOG
                var stat = statistics.ContainsKey(q.Id) ? statistics[q.Id] : null;
                Console.WriteLine($"  Q{q.Id}: Weight={weight:F2} | " +
                    $"Asked={stat?.TimesAsked ?? 0} | " +
                    $"Correct={stat?.TimesCorrect ?? 0} | " +
                    $"Success={stat?.SuccessRate ?? 0:P0}");

                return new { Question = q, Weight = weight };
            }).ToList();

            var selectedQuestions = new List<Question>();
            var random = new Random();
            int questionsToSelect = Math.Min(count, weightedQuestions.Count);

            Console.WriteLine($"\n🎲 Selecting {questionsToSelect} questions:");

            for (int i = 0; i < questionsToSelect; i++)
            {
                double totalWeight = weightedQuestions.Sum(wq => wq.Weight);
                double randomValue = random.NextDouble() * totalWeight;

                double cumulativeWeight = 0;
                var selected = weightedQuestions.First(wq =>
                {
                    cumulativeWeight += wq.Weight;
                    return randomValue <= cumulativeWeight;
                });

                Console.WriteLine($"  [{i + 1}] Q{selected.Question.Id} selected " +
                    $"(Weight: {selected.Weight:F2}, Random: {randomValue:F2}/{totalWeight:F2})");

                selectedQuestions.Add(selected.Question);
                weightedQuestions.Remove(selected);
            }

            Console.WriteLine($"╚═══════════════════════════════════════════════════════════\n");

            return selectedQuestions;
        }

        // SÚLY SZÁMÍTÁS
        private double CalculateWeight(int questionId, Dictionary<int, QuestionStatistic> statistics)
        {
            if (!statistics.ContainsKey(questionId))
            {
                return 1.0; // Alap súly új kérdéseknek
            }

            var stat = statistics[questionId];

            if (stat.TimesAsked == 0)
            {
                return 1.0;
            }

            double successRate = stat.SuccessRate;

            double baseWeight = 1.0 / (successRate + 0.2);

            double incorrectMultiplier = 1.0 + (stat.TimesIncorrect * 0.2);

            double weight = baseWeight * incorrectMultiplier;

            var daysSinceLastAsked = (DateTime.UtcNow - stat.LastAsked).TotalDays;
            if (daysSinceLastAsked < 7 && successRate < 0.6)
            {
                weight *= 1.5;
            }

            return weight;
        }
        public async Task DeleteQuestionsForNoteAsync(int noteId)
        {
            var userId = _authService.GetCurrentUserId();
            if (userId == Guid.Empty)
                throw new InvalidOperationException("User not authenticated");

            using var context = _factory.CreateDbContext();

            // Lekérjük az összes kérdést ehhez a jegyzethez
            var questions = await context.Questions
                .Include(q => q.Answers) // ← FONTOS: Válaszokat is töröljük!
                .Where(q => q.UserId == userId && q.SourceNoteId == noteId)
                .ToListAsync();

            if (questions.Any())
            {
                Console.WriteLine($"🗑️ Törlés: {questions.Count} kérdés a jegyzethez (NoteId: {noteId})");

                context.Questions.RemoveRange(questions);
                await context.SaveChangesAsync();

                Console.WriteLine($"✅ {questions.Count} kérdés sikeresen törölve");
            }
            else
            {
                Console.WriteLine($"ℹ️ Nincs törlendő kérdés (NoteId: {noteId})");
            }
        }
    }
}