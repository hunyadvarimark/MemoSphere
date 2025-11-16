using Core.Entities;
using Core.Interfaces.Services;
using System.Collections.ObjectModel;
using System.Windows;
using WPF.Utilities;
using WPF.ViewModels.Dashboard;  // Keep this if needed elsewhere, but we won't use DashboardViewModel here

namespace WPF.ViewModels.Topics
{
    public class TopicListViewModel : BaseViewModel
    {
        private readonly ITopicService _topicService;
        private readonly IActiveLearningService _activeLearningService;
        // REMOVE: private readonly DashboardViewModel _dashboardViewModel;  // No longer needed for decoupling

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

        // ADD: The missing event (parameterless Action, since the subscriber just needs to know "something changed")
        public event Action TopicActivationChanged;

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

        // CHANGE: Remove DashboardViewModel from constructor params
        public TopicListViewModel(ITopicService topicService, IActiveLearningService activeLearningService)
        {
            _topicService = topicService;
            _activeLearningService = activeLearningService;
            // REMOVE: _dashboardViewModel = dashboardViewModel;

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
            // 1. Témakörök lekérése (mint eddig)
            var topics = await _topicService.GetTopicBySubjectIdAsync(subjectId);
            // 2. Aktív témakörök ID-jainak lekérése (HashSet a gyors kereséshez)
            var activeTopics = await _activeLearningService.GetActiveTopicsAsync();
            var activeTopicIds = activeTopics.Select(at => at.TopicId).ToHashSet();
            // 3. Lista feltöltése és 'IsActive' beállítása
            foreach (var t in topics.OrderBy(x => x.Title))
            {
                var topicVM = new TopicViewModel(t);
                // Ellenőrizzük, hogy ez a téma benne van-e az aktívak listájában
                topicVM.IsActive = activeTopicIds.Contains(t.Id);
                Topics.Add(topicVM);
            }
            SelectedTopic = null;
            // ADD: Optionally raise the event here if loading changes activation states, but probably not needed since dashboard loads separately
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
                    // Szűrés a címek alapján
                    // Ha van IsVisible property, állítsd be
                }
            }

        }
    }
}