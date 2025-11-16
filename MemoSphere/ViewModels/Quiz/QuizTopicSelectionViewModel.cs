using Core.Interfaces.Services;
using System.Collections.ObjectModel;
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
        private readonly QuizViewModel _quizVM;
        private MainViewModel _mainVM;

        public ObservableCollection<SelectableTopicViewModel> SelectableTopics { get; } = new();

        public AsyncCommand<object> StartSelectedQuizCommand { get; }
        public RelayCommand CancelCommand { get; }

        public QuizTopicSelectionViewModel(
            ITopicService topicService,
            IActiveLearningService activeLearningService,
            QuizViewModel quizVM)
        {
            _topicService = topicService;
            _activeLearningService = activeLearningService;
            _quizVM = quizVM;

            StartSelectedQuizCommand = new AsyncCommand<object>(StartSelectedQuizAsync, CanStartSelectedQuiz);
            CancelCommand = new RelayCommand(_ => { if (_mainVM != null) _mainVM.IsQuizSelectionVisible = false; });
        }

        public void Initialize(MainViewModel mainVM)
        {
            _mainVM = mainVM;
        }

        public async Task LoadTopicsAsync(int subjectId)
        {
            SelectableTopics.Clear();

            var topics = await _topicService.GetTopicBySubjectIdAsync(subjectId);

            var activeTopics = await _activeLearningService.GetActiveTopicsAsync();
            var activeTopicIds = activeTopics.Select(at => at.TopicId).ToHashSet();

            foreach (var t in topics.OrderBy(x => x.Title))
            {
                var topicVM = new TopicViewModel(t)
                {
                    IsActive = activeTopicIds.Contains(t.Id)
                };

                var selectableVM = new SelectableTopicViewModel(topicVM);

                if (selectableVM.IsActive)
                {
                    selectableVM.IsSelected = true;
                }

                SelectableTopics.Add(selectableVM);
            }

            StartSelectedQuizCommand.RaiseCanExecuteChanged();
        }

        private bool CanStartSelectedQuiz(object arg)
        {
            return SelectableTopics.Any(t => t.IsSelected);
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

            _mainVM.IsQuizSelectionVisible = false;

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