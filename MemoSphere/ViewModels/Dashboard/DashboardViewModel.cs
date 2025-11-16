using Core.Interfaces.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace WPF.ViewModels.Dashboard
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IActiveLearningService _activeLearningService;

        public ObservableCollection<ActiveTopicDisplayViewModel> ActiveTopics { get; } = new();

        private int _overallStreak;
        public int OverallStreak
        {
            get => _overallStreak;
            set
            {
                if (SetProperty(ref _overallStreak, value))
                {
                    OnPropertyChanged(nameof(HasOverallStreak));
                }
            }
        }

        public bool HasOverallStreak => OverallStreak > 0;

        private int _totalTodayAnswered;
        public int TotalTodayAnswered
        {
            get => _totalTodayAnswered;
            set
            {
                if (SetProperty(ref _totalTodayAnswered, value))
                {
                    OnPropertyChanged(nameof(TotalProgressText));
                    OnPropertyChanged(nameof(TotalProgressPercentage));
                }
            }
        }

        private int _totalDailyGoal;
        public int TotalDailyGoal
        {
            get => _totalDailyGoal;
            set
            {
                if (SetProperty(ref _totalDailyGoal, value))
                {
                    OnPropertyChanged(nameof(TotalProgressText));
                    OnPropertyChanged(nameof(TotalProgressPercentage));
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public string TotalProgressText => $"{TotalTodayAnswered} / {TotalDailyGoal}";

        public double TotalProgressPercentage =>
            TotalDailyGoal > 0 ? Math.Min(100, (double)TotalTodayAnswered / TotalDailyGoal * 100.0) : 0;

        public DashboardViewModel(IActiveLearningService activeLearningService)
        {
            _activeLearningService = activeLearningService ?? throw new ArgumentNullException(nameof(activeLearningService));
        }

        public async Task LoadDashboardDataAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                ActiveTopics.Clear();

                var baseTopics = await _activeLearningService.GetActiveTopicsAsync();

                if (baseTopics == null || !baseTopics.Any())
                {
                    ResetTotals();
                    return;
                }

                // Párhuzamos betöltés minden topic-hoz
                var topicViewModels = await Task.WhenAll(
                    baseTopics.Select(async topic =>
                    {
                        var topicVM = new ActiveTopicDisplayViewModel
                        {
                            TopicId = topic.TopicId,
                            TopicName = topic.Topic.Title,
                            CurrentStreak = topic.CurrentStreak,
                            DailyGoalQuestions = topic.DailyGoalQuestions
                        };

                        // Párhuzamosan lekérjük az adatokat
                        var todayCountTask = _activeLearningService.GetTodayQuestionsCountAsync(topic.TopicId);
                        var masteryTask = _activeLearningService.GetMasteryPercentageAsync(topic.TopicId);

                        await Task.WhenAll(todayCountTask, masteryTask);

                        topicVM.TodayQuestionsAnswered = await todayCountTask;
                        topicVM.MasteryPercentage = await masteryTask;

                        return topicVM;
                    })
                );

                // Hozzáadjuk a topic-okat a collection-höz
                foreach (var topicVM in topicViewModels)
                {
                    ActiveTopics.Add(topicVM);
                }

                // Összesítések kiszámítása
                CalculateTotals(topicViewModels);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Hiba történt az adatok betöltése során.";
                // TODO: Log the exception
                ResetTotals();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CalculateTotals(ActiveTopicDisplayViewModel[] topics)
        {
            TotalTodayAnswered = topics.Sum(t => t.TodayQuestionsAnswered);
            TotalDailyGoal = topics.Sum(t => t.DailyGoalQuestions);
            OverallStreak = topics.Any() ? topics.Max(t => t.CurrentStreak) : 0;
        }

        private void ResetTotals()
        {
            TotalTodayAnswered = 0;
            TotalDailyGoal = 0;
            OverallStreak = 0;
        }

        public async Task RefreshAsync()
        {
            await LoadDashboardDataAsync();
        }
    }
}