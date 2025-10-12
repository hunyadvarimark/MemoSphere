using Core.Entities;
using Core.Interfaces.Repositories;
using Data.Context;
using Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly MemoSphereDbContext _context;

    public UnitOfWork(MemoSphereDbContext context)
    {
        _context = context;
        Subjects = new GenericRepository<Subject>(_context);
        Topics = new GenericRepository<Topic>(_context);
        Notes = new GenericRepository<Note>(_context);
        Questions = new GenericRepository<Question>(_context);
        Answers = new GenericRepository<Answer>(_context);
        NoteChunks = new GenericRepository<NoteChunk>(_context);
    }

    public IGenericRepository<Subject> Subjects { get; private set; }
    public IGenericRepository<Topic> Topics { get; private set; }
    public IGenericRepository<Note> Notes { get; private set; }
    public IGenericRepository<Question> Questions { get; private set; }
    public IGenericRepository<Answer> Answers { get; private set; }
    public IGenericRepository<NoteChunk> NoteChunks { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}