using Core.Context;
using Core.Entities;
using Core.Repositories;
using MemoSphere.Core.Interfaces;
using System;
using System.Threading.Tasks;

public interface IUnitOfWork : IDisposable
{
    ISubjectRepository Subjects { get; }
    ITopicRepository Topics { get; }
    INoteRepository Notes { get; }
    IQuestionRepository Questions { get; }
    IAnswerRepository Answers { get; }
    INoteChunkRepository NoteChunks { get; }
    Task<int> SaveChangesAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly MemoSphereDbContext _context;

    public UnitOfWork(MemoSphereDbContext context)
    {
        _context = context;
        Subjects = new SubjectRepository(_context);
        Topics = new TopicRepository(_context);
        Notes = new NoteRepository(_context);
        Questions = new QuestionRepository(_context);
        Answers = new AnswerRepository(_context);
        NoteChunks = new NoteChunkRepository(_context);
    }

    public ISubjectRepository Subjects { get; private set; }
    public ITopicRepository Topics { get; private set; }
    public INoteRepository Notes { get; private set; }
    public IQuestionRepository Questions { get; private set; }
    public IAnswerRepository Answers { get; private set; }
    public INoteChunkRepository NoteChunks { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}