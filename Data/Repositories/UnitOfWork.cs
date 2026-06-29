using Core.Entities;
using Core.Interfaces.Repositories;
using Data.Context;
using Data.Repositories;
using Microsoft.EntityFrameworkCore;

public class UnitOfWork : IUnitOfWork
{
    private readonly MemoSphereDbContext _context;
    private bool _disposed = false;

    public UnitOfWork(IDbContextFactory<MemoSphereDbContext> factory)
    {
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        _context = factory.CreateDbContext();

        Subjects = new GenericRepository<Subject>(_context);
        Topics = new GenericRepository<Topic>(_context);
        Notes = new GenericRepository<Note>(_context);
        Questions = new GenericRepository<Question>(_context);
        Answers = new GenericRepository<Answer>(_context);
        NoteChunks = new GenericRepository<NoteChunk>(_context);
        QuestionStatistics = new GenericRepository<QuestionStatistic>(_context);
    }

    public IGenericRepository<Subject> Subjects { get; private set; }
    public IGenericRepository<Topic> Topics { get; private set; }
    public IGenericRepository<Note> Notes { get; private set; }
    public IGenericRepository<Question> Questions { get; private set; }
    public IGenericRepository<Answer> Answers { get; private set; }
    public IGenericRepository<NoteChunk> NoteChunks { get; }
    public IGenericRepository<QuestionStatistic> QuestionStatistics { get; private set; }
    public IGenericRepository<ActiveTopic> ActiveTopics { get; private set; }
    public IGenericRepository<DailyProgress> DailyProgresses { get; private set; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}