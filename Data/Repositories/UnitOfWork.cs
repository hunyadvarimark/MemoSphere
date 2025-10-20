using Core.Entities;
using Core.Interfaces.Repositories;
using Data.Context;
using Data.Repositories;
using Microsoft.EntityFrameworkCore;

public class UnitOfWork : IUnitOfWork
{
    private readonly IDbContextFactory<MemoSphereDbContext> _factory;

    public UnitOfWork(IDbContextFactory<MemoSphereDbContext> factory)
    {
        _factory = factory;
        Subjects = new GenericRepository<Subject>(_factory);
        Topics = new GenericRepository<Topic>(_factory);
        Notes = new GenericRepository<Note>(_factory);
        Questions = new GenericRepository<Question>(_factory);
        Answers = new GenericRepository<Answer>(_factory);
        NoteChunks = new GenericRepository<NoteChunk>(_factory);
    }

    public IGenericRepository<Subject> Subjects { get; private set; }
    public IGenericRepository<Topic> Topics { get; private set; }
    public IGenericRepository<Note> Notes { get; private set; }
    public IGenericRepository<Question> Questions { get; private set; }
    public IGenericRepository<Answer> Answers { get; private set; }
    public IGenericRepository<NoteChunk> NoteChunks { get; }

    public async Task<int> SaveChangesAsync()
    {
        // Mivel a repository-k önállóan mentenek, itt nincs mit tenni
        return 0;
    }

    public void Dispose()
    {
        // Nincs mit dispose-olni, mivel nincs shared context
    }
}