using Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace Data.Context
{
    public class MemoSphereDbContext : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>
    {
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<NoteChunk> NoteChunks { get; set; }
        public DbSet<QuestionStatistic> QuestionStatistics { get; set; }
        public DbSet<ActiveTopic> ActiveTopics { get; set; }
        public DbSet<DailyProgress> DailyProgresses { get; set; }

        public MemoSphereDbContext(DbContextOptions<MemoSphereDbContext> options)
           : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Subject>().Property(s => s.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<Topic>().Property(t => t.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<Note>().Property(n => n.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<Question>().Property(q => q.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<Answer>().Property(a => a.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<NoteChunk>().Property(nc => nc.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<QuestionStatistic>().Property(qs => qs.Id).ValueGeneratedOnAdd();

            modelBuilder.Entity<Subject>()
                .HasOne<IdentityUser<Guid>>()
                .WithMany()
                .HasForeignKey(s => s.UserId) 
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuestionStatistic>()
                .HasOne<IdentityUser<Guid>>()
                .WithMany()
                .HasForeignKey(qs => qs.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ActiveTopic>()
                .HasOne<IdentityUser<Guid>>()
                .WithMany()
                .HasForeignKey(at => at.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DailyProgress>()
                .HasOne<IdentityUser<Guid>>()
                .WithMany()
                .HasForeignKey(dp => dp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Question>()
                .HasMany(q => q.Answers)
                .WithOne(a => a.Question)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.SourceNote)
                .WithMany(n => n.Questions)
                .HasForeignKey(q => q.SourceNoteId)
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

            modelBuilder.Entity<QuestionStatistic>()
                .HasOne(qs => qs.Question)
                .WithMany()
                .HasForeignKey(qs => qs.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuestionStatistic>()
                .HasIndex(qs => new { qs.UserId, qs.QuestionId })
                .IsUnique();

            modelBuilder.Entity<QuestionStatistic>()
                .Ignore(qs => qs.SuccessRate);

            modelBuilder.Entity<ActiveTopic>().Property(at => at.Id).ValueGeneratedOnAdd();

            modelBuilder.Entity<ActiveTopic>()
                .HasOne(at => at.Topic)
                .WithMany()
                .HasForeignKey(at => at.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ActiveTopic>()
                .HasIndex(at => new { at.UserId, at.TopicId })
                .IsUnique();

            modelBuilder.Entity<DailyProgress>().Property(dp => dp.Id).ValueGeneratedOnAdd();

            modelBuilder.Entity<DailyProgress>()
                .HasOne<Topic>()
                .WithMany()
                .HasForeignKey(dp => dp.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DailyProgress>()
                .HasIndex(dp => new { dp.UserId, dp.TopicId, dp.Date })
                .IsUnique();
        }
    }
}