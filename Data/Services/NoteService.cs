using Core.Entities;
using Core.Interfaces.Services;


namespace Data.Services
{
    public class NoteService : INoteService
    {
        private readonly IUnitOfWork _unitOfWork;

        public NoteService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<int> AddNoteAsync(Note note)
        {
            if (string.IsNullOrWhiteSpace(note.Content))
            {
                throw new ArgumentException("A jegyzet tartalma nem lehet üres.", nameof(note.Content));
            }

            if (note.TopicId <= 0)
            {
                //throw new ArgumentException("A téma azonosítója érvénytelen.", nameof(note.TopicId));
                // For now, we assign a default TopicId. In a real application, you might want to handle this differently.
                note.TopicId = 1; // Default topic ID
            }

            await _unitOfWork.Notes.AddAsync(note);
            await _unitOfWork.SaveChangesAsync();


            //chuking the note content into smaller parts for better processing
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
            return note.Id;
        }

        public async Task<IEnumerable<Note>> GetNotesByTopicIdAsync(int topicId)
        {
            if (topicId <= 0)
            {
                throw new ArgumentException("A téma azonosítója érvénytelen.", nameof(topicId));
            }

            return await _unitOfWork.Notes.GetFilteredAsync(filter: n => n.TopicId == topicId);
        }

        public async Task DeleteNoteAsync(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("A jegyzet azonosítója érvénytelen.", nameof(id));
            }

            var noteToDelete = await _unitOfWork.Notes.GetByIdAsync(id);
            if (noteToDelete == null)
            {
                throw new ArgumentException("A jegyzet nem található.", nameof(id));
            }

            _unitOfWork.Notes.Remove(noteToDelete);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateNoteAsync(Note note)
        {
            if (string.IsNullOrWhiteSpace(note.Content))
            {
                throw new ArgumentException("A jegyzet tartalma nem lehet üres.", nameof(note.Content));
            }

            var noteToUpdate = await _unitOfWork.Notes.GetByIdAsync(note.Id);
            if (noteToUpdate == null)
            {
                throw new ArgumentException("A jegyzet nem található.", nameof(note.Id));
            }

            noteToUpdate.Content = note.Content;
            await _unitOfWork.SaveChangesAsync();
        }

        private IEnumerable<string> SplitIntoChunks(string text, int chunkSize)
        {
            for (int i = 0; i < text.Length; i += chunkSize)
            {
                yield return text.Substring(i, Math.Min(chunkSize, text.Length - i));
            }
        }
    }
}
