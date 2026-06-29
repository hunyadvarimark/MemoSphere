using Core.Entities;
using Core.Interfaces.Services;

namespace Data.Services
{
    public class NoteService : INoteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;

        public NoteService(IUnitOfWork unitOfWork, IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
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

            await _unitOfWork.Notes.AddAsync(note);
            await _unitOfWork.SaveChangesAsync();

            var chunks = SplitIntoChunks(note.Content, chunkSize: 2000);

            foreach (var chunkText in chunks)
            {
                var noteChunk = new NoteChunk
                {
                    Content = chunkText,
                    NoteId = note.Id
                };
                await _unitOfWork.NoteChunks.AddAsync(noteChunk);
            }

            await _unitOfWork.SaveChangesAsync();

            return note;
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
            await _unitOfWork.SaveChangesAsync();
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

            _unitOfWork.Notes.Update(noteToUpdate);

            var existingQuestions = await _unitOfWork.Questions.GetFilteredAsync(
                filter: q => q.SourceNoteId == note.Id,
                includeProperties: "Answers"
            );

            if (existingQuestions.Any())
            {
                Console.WriteLine($"🗑️ {existingQuestions.Count()} elavult kérdés törlése a jegyzethez (ID: {note.Id})");

                // Először a válaszokat töröljük
                foreach (var question in existingQuestions)
                {
                    if (question.Answers.Any())
                    {
                        _unitOfWork.Answers.RemoveRange(question.Answers);
                    }
                }

                // Majd a kérdéseket
                _unitOfWork.Questions.RemoveRange(existingQuestions);
            }

            var existingChunks = await _unitOfWork.NoteChunks.GetFilteredAsync(nc => nc.NoteId == note.Id);
            if (existingChunks.Any())
            {
                _unitOfWork.NoteChunks.RemoveRange(existingChunks);
            }

            var chunks = SplitIntoChunks(note.Content, chunkSize: 2000);
            foreach (var chunkText in chunks)
            {
                var noteChunk = new NoteChunk
                {
                    Content = chunkText,
                    NoteId = noteToUpdate.Id
                };
                await _unitOfWork.NoteChunks.AddAsync(noteChunk);
            }

            await _unitOfWork.SaveChangesAsync();

            Console.WriteLine($"✅ Jegyzet frissítve (ID: {note.Id}), elavult kérdések törölve");
            return noteToUpdate;
        }

        private IEnumerable<string> SplitIntoChunks(string text, int chunkSize)
        {
            if (string.IsNullOrEmpty(text)) yield break;

            int overlap = 200;
            int i = 0;
            while (i < text.Length)
            {
                int length = Math.Min(chunkSize, text.Length - i);
                yield return text.Substring(i, length);

                i += (chunkSize - overlap);
                if (i + overlap >= text.Length) break;
            }
        }

        public async Task<Note> GetNoteByIdAsync(int id)
        {
            return await _unitOfWork.Notes.GetByIdAsync(id);
        }
    }
}