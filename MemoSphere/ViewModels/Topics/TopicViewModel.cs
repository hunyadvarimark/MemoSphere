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
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        public TopicViewModel(Topic topic)
        {
            Topic = topic;
            _isActive = false;
        }

    }
}