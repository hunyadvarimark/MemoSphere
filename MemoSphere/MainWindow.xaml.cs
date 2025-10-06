using System.Windows;
using Core.Interfaces;
using Core.Entities;
using Core.Context;
using Core.Repositories;
using MemoSphere.Core.Interfaces;

namespace MemoSphere.WPF
{
    public partial class MainWindow : Window
    {
        private readonly IQuestionService _questionService;
        private readonly IAnswerService _answerService;
        private readonly MemoSphereDbContext _dbContext;
        private readonly INoteRepository _noteRepository;
        private readonly ITopicRepository _topicRepository;
        private readonly ISubjectRepository _subjectRepository;
        private readonly INoteChunkRepository _noteChunkRepository;

        public MainWindow(
            IQuestionService questionService,
            IAnswerService answerService,
            MemoSphereDbContext dbContext,
            INoteRepository noteRepository,
            ITopicRepository topicRepository,
            ISubjectRepository subjectRepository,
            INoteChunkRepository noteChunkRepository)

        {
            InitializeComponent();

            _questionService = questionService;
            _answerService = answerService;
            _dbContext = dbContext;
            _noteRepository = noteRepository;
            _topicRepository = topicRepository;
            _subjectRepository = subjectRepository;
            _noteChunkRepository = noteChunkRepository;

            AddTestNoteAsync();
        }

        private async Task AddTestNoteAsync()
        {
            // Ellenőrizd, hogy az 1-es azonosítójú Note létezik-e már.
            // Ha nem, akkor hozzáadjuk az összes teszt adatot.
            if (await _noteRepository.GetByIdAsync(1) == null)
            {
                // 1. Lépés: Hozd létre és add hozzá a Subject-et.
                var testSubject = new Subject { Name = "Science" };
                await _subjectRepository.AddAsync(testSubject);
                await _dbContext.SaveChangesAsync();

                // 2. Lépés: Hozd létre és add hozzá a Topic-ot.
                // Csatold a Topic-ot a Subject-hez (az entitást is, nem csak az ID-t).
                var testTopic = new Topic { Name = "Physics", Subject = testSubject };
                await _topicRepository.AddAsync(testTopic);
                await _dbContext.SaveChangesAsync();

                // 3. Lépés: Hozd létre és add hozzá a Note-ot.
                // Csatold a Note-ot a Topic-hoz.
                var testNote = new Note
                {
                    Content = "Einstein's theory of relativity has two main parts: special relativity and general relativity. Special relativity deals with the relationship between space and time for objects moving at a constant velocity. General relativity expands on this to include gravity.",
                    Topic = testTopic
                };
                await _noteRepository.AddAsync(testNote);
                await _dbContext.SaveChangesAsync();

                var testNoteChunk = new NoteChunk
                {
                    Content = testNote.Content,
                    NoteId = testNote.Id
                };
                await _noteChunkRepository.AddAsync(testNoteChunk); // Add the chunk
                await _dbContext.SaveChangesAsync(); // Save the new chunk
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _questionService.GenerateAndSaveQuestionsAsync(1);
                MessageBox.Show("Kérdések sikeresen generálva és mentve lettek!", "Siker", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba történt: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}