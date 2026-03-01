using Core.Entities;
using Core.Interfaces.Services;
using System.Collections.ObjectModel;
using System.Windows;
using WPF.Utilities;
using WPF.ViewModels.Dashboard;

namespace WPF.ViewModels.Topics
{
    public class TopicListViewModel : BaseViewModel
    {
        private readonly ITopicService _topicService;
        private readonly IActiveLearningService _activeLearningService;

        public ObservableCollection<TopicViewModel> Topics { get; } = new();
        private TopicViewModel _selectedTopic;
        public RelayCommand EditTopicCommand { get; }
        public RelayCommand DeleteTopicCommand { get; }
        public RelayCommand ActivateTopicCommand { get; }
        public RelayCommand DeactivateTopicCommand { get; }
        public RelayCommand SelectTopicCommand { get; }
        public event Action<Topic> EditTopicRequested;
        public event Action<int> DeleteTopicRequested;
        public event Action<TopicViewModel> TopicSelected;

        public event Action TopicActivationChanged;

        public TopicViewModel SelectedTopic
        {
            get => _selectedTopic;
            set
            {
                if (_selectedTopic != value)
                {
                    if (_selectedTopic != null)
                        _selectedTopic.IsSelected = false;

                    _selectedTopic = value;

                    if (_selectedTopic != null)
                        _selectedTopic.IsSelected = true;

                    OnPropertyChanged(nameof(SelectedTopic));
                    TopicSelected?.Invoke(value);
                }
            }
        }


        public TopicListViewModel(ITopicService topicService, IActiveLearningService activeLearningService)
        {
            _topicService = topicService;
            _activeLearningService = activeLearningService;

            SelectTopicCommand = new RelayCommand(
                 param =>
                 {
                     if (param is TopicViewModel vm)
                     {
                         SelectedTopic = vm;
                     }
                 }
             );
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

            ActivateTopicCommand = new RelayCommand(
                async (param) =>
                {
                    if (param is TopicViewModel topicVM)
                    {
                        // 1. Service hívása
                        await _activeLearningService.ActivateTopicAsync(topicVM.Id);
                        // 2. UI azonnali frissítése
                        topicVM.IsActive = true;
                        // CHANGE: Raise the event instead of direct dashboard call
                        TopicActivationChanged?.Invoke();
                    }
                },
                param => param is TopicViewModel);

            DeactivateTopicCommand = new RelayCommand(
                async (param) =>
                {
                    if (param is TopicViewModel topicVM)
                    {
                        // 1. Service hívása
                        await _activeLearningService.DeactivateTopicAsync(topicVM.Id);
                        // 2. UI azonnali frissítése
                        topicVM.IsActive = false;
                        // CHANGE: Raise the event instead of direct dashboard call
                        TopicActivationChanged?.Invoke();
                    }
                },
                param => param is TopicViewModel);
        }

        public async Task LoadTopicsAsync(int subjectId)
        {
            Topics.Clear();
            var topics = await _topicService.GetTopicBySubjectIdAsync(subjectId);
            var activeTopics = await _activeLearningService.GetActiveTopicsAsync();
            var activeTopicIds = activeTopics.Select(at => at.TopicId).ToHashSet();
            
            foreach (var t in topics.OrderBy(x => x.Title))
            {
                var topicVM = new TopicViewModel(t);
                topicVM.IsActive = activeTopicIds.Contains(t.Id);
                Topics.Add(topicVM);
            }
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
        public void FilterTopics(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                foreach (var topic in Topics)
                {
                }
            }
            else
            {
                var lowerSearch = searchText.ToLower();
                foreach (var topic in Topics)
                {

                }
            }

        }

    }
}