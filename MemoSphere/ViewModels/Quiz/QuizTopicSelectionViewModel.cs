// ViewModels/Quiz/QuizTopicSelectionViewModel.cs
using Core.Interfaces.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WPF.Utilities;
using WPF.ViewModels.Topics;
using WPF.Views.Quiz;

namespace WPF.ViewModels.Quiz
{
    public class QuizTopicSelectionViewModel : BaseViewModel
    {
        private readonly ITopicService _topicService;
        private readonly IActiveLearningService _activeLearningService;
        private readonly IQuizService _quizService;
        private readonly QuizViewModel _quizVM;
        private MainViewModel _mainVM;

        public ObservableCollection<SelectableTopicViewModel> SelectableTopics { get; } = new();

 
        private int _selectedCount;
        public int SelectedCount
        {
            get => _selectedCount;
            set => SetProperty(ref _selectedCount, value);
        }

        public AsyncCommand<object> StartSelectedQuizCommand { get; }
        public RelayCommand CancelCommand { get; }

        public RelayCommand SelectAllCommand { get; }
        public RelayCommand DeselectAllCommand { get; }

        public QuizTopicSelectionViewModel(
            ITopicService topicService,
            IActiveLearningService activeLearningService,
            IQuizService quizService,
            QuizViewModel quizVM)
        {
            _topicService = topicService;
            _activeLearningService = activeLearningService;
            _quizService = quizService;
            _quizVM = quizVM;

            StartSelectedQuizCommand = new AsyncCommand<object>(StartSelectedQuizAsync, CanStartSelectedQuiz);
            CancelCommand = new RelayCommand(_ => { if (_mainVM != null) _mainVM.CurrentMainView = MainViewType.Browser; });

            SelectAllCommand = new RelayCommand(SelectAll);
            DeselectAllCommand = new RelayCommand(DeselectAll);

            SelectableTopics.CollectionChanged += SelectableTopics_CollectionChanged;
        }

        public void Initialize(MainViewModel mainVM)
        {
            _mainVM = mainVM;
        }

        public async Task LoadTopicsAsync(int subjectId)
        {
            foreach (var item in SelectableTopics)
                item.PropertyChanged -= SelectableTopic_PropertyChanged;

            SelectableTopics.Clear();

            var topics = await _topicService.GetTopicBySubjectIdAsync(subjectId);
            var activeTopics = await _activeLearningService.GetActiveTopicsAsync();
            var activeTopicIds = activeTopics.Select(at => at.TopicId).ToHashSet();

            foreach (var t in topics.OrderBy(x => x.Title))
            {
                var topicVM = new TopicViewModel(t);
                topicVM.IsActive = activeTopicIds.Contains(t.Id);

                var selectableVM = new SelectableTopicViewModel(topicVM);

                // Adatbetöltés a kártyához
                selectableVM.QuestionCount = await _quizService.GetQuestionCountForTopicsAsync(new List<int> { t.Id });
                selectableVM.MasteryPercentage = await _activeLearningService.GetMasteryPercentageAsync(t.Id);

                if (selectableVM.IsActive)
                {
                    selectableVM.IsSelected = true;
                }

                // Feliratkozunk az IsSelected változásra
                selectableVM.PropertyChanged += SelectableTopic_PropertyChanged;
                SelectableTopics.Add(selectableVM);
            }

            UpdateSelectedCount(); // Kezdeti számolás
            StartSelectedQuizCommand.RaiseCanExecuteChanged();
        }

        // --- Gyors Műveletek ---
        private void SelectAll(object obj)
        {
            foreach (var item in SelectableTopics) item.IsSelected = true;
        }
        private void DeselectAll(object obj)
        {
            foreach (var item in SelectableTopics) item.IsSelected = false;
        }

        // --- Kijelölés számolása ---
        private void SelectableTopics_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (SelectableTopicViewModel item in e.NewItems)
                    item.PropertyChanged += SelectableTopic_PropertyChanged;

            if (e.OldItems != null)
                foreach (SelectableTopicViewModel item in e.OldItems)
                    item.PropertyChanged -= SelectableTopic_PropertyChanged;

            UpdateSelectedCount();
        }

        private void SelectableTopic_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectableTopicViewModel.IsSelected))
            {
                UpdateSelectedCount();
            }
        }

        private void UpdateSelectedCount()
        {
            SelectedCount = SelectableTopics.Count(t => t.IsSelected);
            // Frissítjük a gomb állapotát
            StartSelectedQuizCommand.RaiseCanExecuteChanged();
        }

        private bool CanStartSelectedQuiz(object arg)
        {
            // Most már a SelectedCount-tól függ
            return SelectedCount > 0;
        }

        private async Task StartSelectedQuizAsync(object arg)
        {
            var selectedTopicIds = SelectableTopics
                .Where(t => t.IsSelected)
                .Select(t => t.TopicVM.Id)
                .ToList();

            if (!selectedTopicIds.Any())
            {
                MessageBox.Show("Válassz ki legalább egy témakört a kvíz indításához.", "Nincs kiválasztva témakör", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _mainVM.CurrentMainView = MainViewType.Browser; // Váltsunk vissza a böngészőre

            await _quizVM.LoadQuizCommand.ExecuteAsync(selectedTopicIds);


            if (_quizVM.QuizItems != null && _quizVM.QuizItems.Any())
            {
                System.Diagnostics.Debug.WriteLine($"✅ Multi-topic quiz loaded with {_quizVM.QuizItems.Count} questions");

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var quizWindow = new QuizWindow(_quizVM);
                    quizWindow.ShowDialog();
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Multi-topic quiz load failed or returned no questions.");
            }
        }
    }
}