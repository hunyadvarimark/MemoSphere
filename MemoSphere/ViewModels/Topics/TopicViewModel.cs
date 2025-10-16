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
        public TopicViewModel(Topic topic)
        {
            Topic = topic;
        }
    }
}