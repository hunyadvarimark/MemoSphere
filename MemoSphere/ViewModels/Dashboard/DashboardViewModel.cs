using Core.Interfaces.Services;
using System.Collections.ObjectModel;

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
            set => SetProperty(ref _totalTodayAnswered, value);
        }

        private int _totalDailyGoal;
        public int TotalDailyGoal
        {
            get => _totalDailyGoal;
            set => SetProperty(ref _totalDailyGoal, value);
        }

        public string TotalProgressText => $"{TotalTodayAnswered} / {TotalDailyGoal}";
        public double TotalProgressPercentage => TotalDailyGoal > 0 ? ((double)TotalTodayAnswered / TotalDailyGoal * 100.0) : 0;

        public DashboardViewModel(IActiveLearningService activeLearningService)
        {
            _activeLearningService = activeLearningService;
        }

        public async Task LoadDashboardDataAsync()
        {
            ActiveTopics.Clear();

            var baseTopics = await _activeLearningService.GetActiveTopicsAsync();

            int totalAnswered = 0;
            int totalGoal = 0;
            int maxStreak = 0;

            foreach (var topic in baseTopics)
            {
                var topicVM = new ActiveTopicDisplayViewModel
                {
                    TopicId = topic.TopicId,
                    TopicName = topic.Topic.Title,
                    CurrentStreak = topic.CurrentStreak,
                    DailyGoalQuestions = topic.DailyGoalQuestions
                };

                topicVM.TodayQuestionsAnswered = await _activeLearningService.GetTodayQuestionsCountAsync(topic.TopicId);
                topicVM.MasteryPercentage = await _activeLearningService.GetMasteryPercentageAsync(topic.TopicId);

                ActiveTopics.Add(topicVM);

                totalAnswered += topicVM.TodayQuestionsAnswered;
                totalGoal += topicVM.DailyGoalQuestions;
                if (topicVM.CurrentStreak > maxStreak)
                {
                    maxStreak = topicVM.CurrentStreak;
                }
            }

            TotalTodayAnswered = totalAnswered;
            TotalDailyGoal = totalGoal;
            OverallStreak = maxStreak;

            OnPropertyChanged(nameof(TotalProgressText));
            OnPropertyChanged(nameof(TotalProgressPercentage));
        }
    }
}