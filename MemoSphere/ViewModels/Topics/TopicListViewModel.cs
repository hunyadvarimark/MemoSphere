using Core.Entities;
using Core.Interfaces.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WPF.Utilities;

namespace WPF.ViewModels.Topics
{
    public class TopicListViewModel : BaseViewModel
    {
        private readonly ITopicService _topicService;
        public ObservableCollection<TopicViewModel> Topics { get; } = new();
        private TopicViewModel _selectedTopic;

        public RelayCommand EditTopicCommand { get; }
        public RelayCommand DeleteTopicCommand { get; }

        public event Action<Topic> EditTopicRequested;
        public event Action<int> DeleteTopicRequested;
        public event Action<TopicViewModel> TopicSelected;

        public TopicViewModel SelectedTopic
        {
            get => _selectedTopic;
            set
            {
                if (_selectedTopic != value)
                {
                    _selectedTopic = value;
                    OnPropertyChanged(nameof(SelectedTopic));
                    TopicSelected?.Invoke(value);
                }
            }
        }

        public TopicListViewModel(ITopicService topicService)
        {
            _topicService = topicService;
            EditTopicCommand = new RelayCommand(
                param => { if (param is TopicViewModel topicVM) EditTopicRequested?.Invoke(topicVM.Topic); },
                param => param is TopicViewModel);
            DeleteTopicCommand = new RelayCommand(
                param =>
                {
                    if (param is TopicViewModel topicVM)
                    {
                        var result = MessageBox.Show(
                            $"Biztosan törölni szeretnéd a '{topicVM.Title}' témakört?\n\nFigyelem: Az ehhez tartozó összes jegyzet is törlődni fog!",
                            "Törlés megerősítése",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            DeleteTopicRequested?.Invoke(topicVM.Id);
                        }
                    }
                },
                param => param is TopicViewModel);
        }

        public async Task LoadTopicsAsync(int subjectId)
        {
            Topics.Clear();
            var topics = await _topicService.GetTopicBySubjectIdAsync(subjectId);
            foreach (var t in topics.OrderBy(x => x.Title))
                Topics.Add(new TopicViewModel(t));
            SelectedTopic = null;
        }

        public void ClearTopics()
        {
            Topics.Clear();
        }

        public void RemoveTopic(int topicId)
        {
            var topicToRemove = Topics.FirstOrDefault(t => t.Id == topicId);
            if (topicToRemove != null)
            {
                Topics.Remove(topicToRemove);
                SelectedTopic = null;
            }
        }
    }
}