// WPF/ViewModels/Dashboard/ActiveTopicDisplayViewModel.cs
using WPF.Utilities;

namespace WPF.ViewModels.Dashboard
{
    public class ActiveTopicDisplayViewModel : BaseViewModel
    {
        public int TopicId { get; set; }
        public string TopicName { get; set; }
        public int CurrentStreak { get; set; }

        private double _masteryPercentage;
        public double MasteryPercentage
        {
            get => _masteryPercentage;
            set => SetProperty(ref _masteryPercentage, value);
        }

        private int _todayQuestionsAnswered;
        public int TodayQuestionsAnswered
        {
            get => _todayQuestionsAnswered;
            set => SetProperty(ref _todayQuestionsAnswered, value);
        }

        private int _dailyGoalQuestions;
        public int DailyGoalQuestions
        {
            get => _dailyGoalQuestions;
            set => SetProperty(ref _dailyGoalQuestions, value);
        }

        public bool IsGoalReachedToday => TodayQuestionsAnswered >= DailyGoalQuestions;
        public double DailyProgressPercentage => DailyGoalQuestions > 0 ? ((double)TodayQuestionsAnswered / DailyGoalQuestions * 100.0) : 0;
        public string DailyProgressText => $"{TodayQuestionsAnswered} / {DailyGoalQuestions}";
    }
}