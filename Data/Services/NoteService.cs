using Core.Entities;
using Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Data.Context;

namespace Data.Services
{
    public class NoteService : INoteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;
        private readonly IDbContextFactory<MemoSphereDbContext> _factory;

        public NoteService(IUnitOfWork unitOfWork, IAuthService authService, IDbContextFactory<MemoSphereDbContext> factory)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
            _factory = factory;
        }

        public async Task<Note> AddNoteAsync(Note note)
        {
            var userId = _authService.GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(note.Content))
            {
                throw new ArgumentException("A jegyzet tartalma nem lehet üres.", nameof(note.Content));
            }

            if (note.TopicId <= 0)
            {
                throw new ArgumentException("A jegyzetnek érvényes témakörhöz kell tartoznia.", nameof(note.TopicId));
            }

            note.UserId = userId;

            using var context = _factory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                context.Notes.Add(note);
                await context.SaveChangesAsync();

                var chunks = SplitIntoChunks(note.Content, chunkSize: 2000);

                foreach (var chunkText in chunks)
                {
                    var noteChunk = new NoteChunk
                    {
                        Content = chunkText,
                        NoteId = note.Id
                    };
                    context.NoteChunks.Add(noteChunk);
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return note;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<Note>> GetNotesByTopicIdAsync(int topicId)
        {
            var userId = _authService.GetCurrentUserId();

            if (topicId <= 0)
            {
                throw new ArgumentException("A téma azonosítója érvénytelen.", nameof(topicId));
            }

            return await _unitOfWork.Notes.GetFilteredAsync(filter: n => n.TopicId == topicId && n.UserId == userId);
        }

        public async Task DeleteNoteAsync(int id)
        {
            var userId = _authService.GetCurrentUserId();

            if (id <= 0)
            {
                throw new ArgumentException("A jegyzet azonosítója érvénytelen.", nameof(id));
            }

            var noteToDelete = await _unitOfWork.Notes.GetByIdAsync(id);
            if (noteToDelete == null || noteToDelete.UserId != userId)
            {
                throw new ArgumentException("Jegyzet törlése hiba: A jegyzet nem található vagy nincs jogosultság a törléséhez.", nameof(id));
            }

            _unitOfWork.Notes.Remove(noteToDelete);
        }

        public async Task<Note> UpdateNoteAsync(Note note)
        {
            var userId = _authService.GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(note.Content))
            {
                throw new ArgumentException("A jegyzet tartalma nem lehet üres.", nameof(note.Content));
            }

            var noteToUpdate = await _unitOfWork.Notes.GetByIdAsync(note.Id);
            if (noteToUpdate == null || noteToUpdate.UserId != userId)
            {
                throw new ArgumentException("Jegyzet frissítése hiba: A jegyzet nem található vagy nincs jogosultság a frissítéséhez.", nameof(note.Id));
            }

            noteToUpdate.Content = note.Content;
            if (!string.IsNullOrWhiteSpace(note.Title))
            {
                noteToUpdate.Title = note.Title;
            }
            if (note.TopicId > 0)
            {
                noteToUpdate.TopicId = note.TopicId;
            }

            using var context = _factory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                context.Notes.Update(noteToUpdate);

                var existingQuestions = await context.Questions
                    .Include(q => q.Answers)
                    .Where(q => q.SourceNoteId == note.Id)
                    .ToListAsync();

                if (existingQuestions.Any())
                {
                    Console.WriteLine($"🗑️ {existingQuestions.Count} elavult kérdés törlése a jegyzethez (ID: {note.Id})");

                    // Először a válaszokat töröljük
                    foreach (var question in existingQuestions)
                    {
                        if (question.Answers.Any())
                        {
                            context.Answers.RemoveRange(question.Answers);
                        }
                    }

                    // Majd a kérdéseket
                    context.Questions.RemoveRange(existingQuestions);
                }

                var existingChunks = await context.NoteChunks.Where(nc => nc.NoteId == note.Id).ToListAsync();
                if (existingChunks.Any())
                {
                    context.NoteChunks.RemoveRange(existingChunks);
                }

                var chunks = SplitIntoChunks(note.Content, chunkSize: 2000);
                foreach (var chunkText in chunks)
                {
                    var noteChunk = new NoteChunk
                    {
                        Content = chunkText,
                        NoteId = noteToUpdate.Id
                    };
                    context.NoteChunks.Add(noteChunk);
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                Console.WriteLine($"✅ Jegyzet frissítve (ID: {note.Id}), elavult kérdések törölve");
                return noteToUpdate;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private IEnumerable<string> SplitIntoChunks(string text, int chunkSize)
        {
            if (string.IsNullOrEmpty(text))
            {
                yield break;
            }

            for (int i = 0; i < text.Length; i += chunkSize)
            {
                yield return text.Substring(i, Math.Min(chunkSize, text.Length - i));
            }
        }

        public async Task<Note> GetNoteByIdAsync(int id)
        {
            return await _unitOfWork.Notes.GetByIdAsync(id);
        }
    }
}