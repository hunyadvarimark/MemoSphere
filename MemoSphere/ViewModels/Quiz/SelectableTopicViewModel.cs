// ViewModels/Quiz/SelectableTopicViewModel.cs
using System.Windows.Input;
using WPF.Utilities;
using WPF.ViewModels.Topics;

namespace WPF.ViewModels.Quiz
{
    public class SelectableTopicViewModel : BaseViewModel
    {
        public TopicViewModel TopicVM { get; }

        // Átvezető property-k a UI számára
        public string Title => TopicVM.Title;
        public bool IsActive => TopicVM.IsActive;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private int _questionCount;
        public int QuestionCount
        {
            get => _questionCount;
            set => SetProperty(ref _questionCount, value);
        }

        private double _masteryPercentage;
        public double MasteryPercentage
        {
            get => _masteryPercentage;
            set => SetProperty(ref _masteryPercentage, value);
        }

        public RelayCommand ToggleSelectionCommand { get; }

        public SelectableTopicViewModel(TopicViewModel topicVM)
        {
            TopicVM = topicVM;
            IsSelected = false;
            ToggleSelectionCommand = new RelayCommand(_ => IsSelected = !IsSelected);
        }
    }
}