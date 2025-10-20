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

            modelBuilder.Entity<Subject>().Property(s => s.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<Topic>().Property(t => t.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<Note>().Property(n => n.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<Question>().Property(q => q.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<Answer>().Property(a => a.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<NoteChunk>().Property(nc => nc.Id).ValueGeneratedOnAdd();

            modelBuilder.Entity<Question>()
                .HasMany(q => q.Answers)
                .WithOne(a => a.Question)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.SourceNote)
                .WithMany()
                .HasForeignKey(q => q.SourceNoteId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NoteChunk>()
                .HasOne<Note>()
                .WithMany()
                .HasForeignKey(nc => nc.NoteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Note>()
                .HasOne(n => n.Topic)
                .WithMany(t => t.Notes)
                .HasForeignKey(n => n.TopicId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<Topic>()
                .HasOne(t => t.Subject)
                .WithMany(s => s.Topics)
                .HasForeignKey(t => t.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Answer>()
                .Property(a => a.IsCorrect)
                .HasConversion<int>();
        }
    }
}