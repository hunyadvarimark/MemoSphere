using Core.Entities;
using Core.Interfaces.Services;
using System.Diagnostics;
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
        private readonly MainViewModel _viewModel;

        public MainWindow(
            MainViewModel viewModel,
            ISubjectService subjectService,
            ITopicService topicService,
            INoteService noteService)
        {

            _subjectService = subjectService;
            _topicService = topicService;
            _noteService = noteService;
            _viewModel = viewModel;

            DataContext = _viewModel;
            InitializeComponent();
            // NE HÍVD MEG ITT A BETÖLTÉST! A konstruktor túl korán fut, amikor még nincs session.
        }

        public async Task LoadDataAsync()
        {
            await InitializeDataAndRefreshAsync(_viewModel);
        }

        private async Task InitializeDataAndRefreshAsync(MainViewModel mainViewModel)
        {
            try
            {
                //await EnsureTestDataExistsAsync();

                await mainViewModel.InitializeAsync();

                // Távolítsuk el a manuális re-set-eket és delay-eket – hagyjuk az eseménykezelőkre (HierarchyCoordinator)
                // Ha szükséges, várjunk a teljes inicializációra, de jobb async Task.WhenAll vagy similar használatával
                // Itt nincs szükség további manuális load-ra, mert az InitializeAsync() után az első SelectedSubject triggereli a láncot
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Startup error: {ex}");
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}