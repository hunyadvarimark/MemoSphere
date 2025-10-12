using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Data.Context
{
    public class MemoSphereDbContext : DbContext
    {
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<NoteChunk> NoteChunks { get; set; }


        public MemoSphereDbContext(DbContextOptions<MemoSphereDbContext> options)
           : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Question>()
                .HasMany(q => q.Answers)
                .WithOne(a => a.Question)
                .HasForeignKey(a => a.QuestionId);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.SourceNote)
                .WithMany()
                .HasForeignKey(q => q.SourceNoteId)
                .IsRequired(false);

            modelBuilder.Entity<Answer>()
                .Property(a => a.IsCorrect)
                .HasConversion<int>();
        }
    }
}
