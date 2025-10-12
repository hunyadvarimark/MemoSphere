
using Core.Entities;
using Core.Interfaces.Services;
using System.Windows;
using WPF.Utilities;
using WPF.ViewModels;

namespace MemoSphere.WPF
{
    public partial class MainWindow : Window
    {
        private readonly ISubjectService _subjectService;
        private readonly ITopicService _topicService;
        private readonly INoteService _noteService;

        public MainWindow(
            MainViewModel viewModel,
            ISubjectService subjectService,
            ITopicService topicService, 
            INoteService noteService)
        {
            InitializeComponent();

            _subjectService = subjectService;
            _topicService = topicService;
            _noteService = noteService;

            DataContext = viewModel;

            _ = InitializeDataAndRefreshAsync(viewModel);
        }
        private async Task InitializeDataAndRefreshAsync(MainViewModel mainViewModel)
        {
            try
            {
                await EnsureTestDataExistsAsync();

                if (mainViewModel.HierarchyVM?.LoadSubjectsCommand is AsyncCommand<object> loadSubjectsCommand)
                {
                    if (loadSubjectsCommand.CanExecute(null))
                    {
                        await loadSubjectsCommand.ExecuteAsync(null);
                    }
                }

                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Alkalmazás inicializálási hiba: {ex.Message}");
            }
        }

        private async Task EnsureTestDataExistsAsync()
        {
            try
            {
                // A mezőket használjuk, amiket a konstruktorban injektáltunk
                var anySubjectExists = (await _subjectService.GetAllSubjectsAsync()).Any();

                if (anySubjectExists)
                {
                    return;
                }

                // ----------------------------------------------------
                // 🏆 1. TÁRGY LÉTREHOZÁSA (Science)
                // ----------------------------------------------------
                var scienceSubject = await _subjectService.AddSubjectAsync("Science");
                if (scienceSubject == null) return;

                // 1.1 TÉMAKÖR LÉTREHOZÁSA A Science-hez (Physics)
                var tempTopic = new Topic
                {
                    Title = "Physics",
                    SubjectId = scienceSubject.Id
                };
                await _topicService.AddTopicAsync(tempTopic);

                var physicsTopic = (await _topicService.GetTopicBySubjectIdAsync(scienceSubject.Id))
                                                         .FirstOrDefault(t => t.Title == "Physics");
                if (physicsTopic == null) return;

                // 1.2 JEGYZET LÉTREHOZÁSA a Physics-hez
                var physicsNote = new Note
                {
                    Title = "Einstein's Theory of Relativity",
                    Content = "Einstein's theory of relativity has two main parts: special relativity and general relativity...",
                    TopicId = physicsTopic.Id,
                };
                await _noteService.AddNoteAsync(physicsNote);


                // ----------------------------------------------------
                // 🏆 2. TÁRGY LÉTREHOZÁSA (History)
                // ----------------------------------------------------
                var historySubject = await _subjectService.AddSubjectAsync("History");
                if (historySubject == null) return;

                // 2.1 TÉMAKÖR LÉTREHOZÁSA a History-hoz (Ancient Rome)
                tempTopic = new Topic
                {
                    Title = "Ancient Rome",
                    SubjectId = historySubject.Id
                };
                await _topicService.AddTopicAsync(tempTopic);

                var romeTopic = (await _topicService.GetTopicBySubjectIdAsync(historySubject.Id))
                                                     .FirstOrDefault(t => t.Title == "Ancient Rome");
                if (romeTopic == null) return;

                // 2.2 JEGYZET LÉTREHOZÁSA az Ancient Rome-hoz
                var romeNote = new Note
                {
                    Title = "The Fall of the Western Roman Empire",
                    Content = "The traditional date for the fall of the Western Roman Empire is 476 AD, when the last emperor, Romulus Augustulus, was deposed...",
                    TopicId = romeTopic.Id,
                };
                await _noteService.AddNoteAsync(romeNote);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tesztadat feltöltési hiba: {ex.Message}");
            }
        }
    }
}