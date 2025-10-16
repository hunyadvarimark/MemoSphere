using Core.Entities;
using System.Windows.Input;
using WPF.Utilities;

namespace WPF.ViewModels.Topics
{
    public class TopicDetailViewModel : BaseViewModel
    {
        private Topic _currentTopic;
        private int _parentSubjectId;
        private string _topicTitle = string.Empty;

        public string TopicTitle
        {
            get => _topicTitle;
            set
            {
                if (SetProperty(ref _topicTitle, value))
                {
                    SaveTopicCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public AsyncCommand<object> SaveTopicCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<Topic> TopicSavedRequested;
        public event Action CancelRequested;

        public TopicDetailViewModel()
        {
            SaveTopicCommand = new AsyncCommand<object>(SaveTopicRequestedAsync, CanSaveTopic);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        public void ResetState(int parentSubjectId)
        {
            _currentTopic = null;
            _parentSubjectId = parentSubjectId;
            TopicTitle = string.Empty;
            SaveTopicCommand.RaiseCanExecuteChanged();
        }

        public void LoadTopic(Topic topicToEdit)
        {
            _currentTopic = topicToEdit;
            TopicTitle = topicToEdit?.Title ?? string.Empty;
            _parentSubjectId = topicToEdit?.SubjectId ?? 0;
            SaveTopicCommand.RaiseCanExecuteChanged();
        }

        private bool CanSaveTopic(object parameter)
        {
            return !string.IsNullOrWhiteSpace(TopicTitle) && _parentSubjectId > 0;
        }

        private Task SaveTopicRequestedAsync(object parameter)
        {
            if (!CanSaveTopic(null)) return Task.CompletedTask;

            string trimmedTitle = TopicTitle.Trim();
            Topic topicToSave = _currentTopic ?? new Topic { SubjectId = _parentSubjectId };
            topicToSave.Title = trimmedTitle;

            // Event kiváltása - a MainViewModel fogja lekezelni a mentést és a reset-et
            TopicSavedRequested?.Invoke(topicToSave);

            return Task.CompletedTask;
        }

        private void ExecuteCancel(object parameter)
        {
            CancelRequested?.Invoke();
            ResetState(_parentSubjectId); // Reset a modal bezárásakor
        }
    }
}