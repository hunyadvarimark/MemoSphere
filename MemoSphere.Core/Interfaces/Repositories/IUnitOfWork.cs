using Core.Entities;
using Core.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<Subject> Subjects { get; }
    IGenericRepository<Topic> Topics { get; }
    IGenericRepository<Note> Notes { get; }
    IGenericRepository<Question> Questions { get; }
    IGenericRepository<Answer> Answers { get; }
    IGenericRepository<NoteChunk> NoteChunks { get; }
    Task<int> SaveChangesAsync();
}
