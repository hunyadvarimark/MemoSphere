using Core.Entities;
using WPF.Utilities;

namespace WPF.ViewModels.Topics
{
    public class TopicViewModel : BaseViewModel
    {
        public Topic Topic { get; }

        public int Id => Topic.Id;
        public string Title
        {
            get => Topic.Title;
            set
            {
                Topic.Title = value;
                OnPropertyChanged();
            }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged(nameof(IsActive));
                }
            }
        }
        public TopicViewModel(Topic topic)
        {
            Topic = topic;
            _isActive = false;
        }
    }
}