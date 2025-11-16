using WPF.ViewModels.Topics;

namespace WPF.ViewModels.Quiz
{
    public class SelectableTopicViewModel : BaseViewModel
    {
        public TopicViewModel TopicVM { get; }

        public string Title => TopicVM.Title;
        public bool IsActive => TopicVM.IsActive;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public SelectableTopicViewModel(TopicViewModel topicVM)
        {
            TopicVM = topicVM;
            IsSelected = false;
        }
    }
}