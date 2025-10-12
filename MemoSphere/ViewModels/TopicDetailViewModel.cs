using Core.Entities;
using Core.Interfaces.Services;
using System.Security.Cryptography;
using System.Windows.Input;
using WPF.Utilities;

namespace WPF.ViewModels
{
    public class TopicDetailViewModel : BaseViewModel
    {
        private readonly ITopicService _topicService;

        private Topic _currentTopic;
        private int _parentSubjectId;

        private string _topicTitle;
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

        //Commands
        public AsyncCommand<object> SaveTopicCommand { get; }
        public ICommand CancelCommand { get; }

        //Events
        public event Action<Topic> TopicSavedSuccessfully;
        public event Action CancelRequested;

        public TopicDetailViewModel(ITopicService topicService)
        {
            _topicService = topicService;
            SaveTopicCommand = new AsyncCommand<object>(SaveTopicAsync, CanSaveTopic);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        //Methods
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
            TopicTitle = topicToEdit.Title;
            _parentSubjectId = topicToEdit.SubjectId;

            SaveTopicCommand.RaiseCanExecuteChanged();
        }
        public void RaiseCanExecuteChanged()
        {
            SaveTopicCommand.RaiseCanExecuteChanged();
        }
        private bool CanSaveTopic(object parameter)
        {
            return !string.IsNullOrWhiteSpace(TopicTitle) && _parentSubjectId > 0;
        }
        private async Task SaveTopicAsync(object parameter)
        {
            if (!CanSaveTopic(null)) return;

            try
            {
                Topic resultTopic;
                string trimmedTitle = TopicTitle.Trim();

                int? excludeId = _currentTopic?.Id;
                if (await _topicService.TopicExistsAsync(trimmedTitle, _parentSubjectId, excludeId))
                {
                    Console.WriteLine("Hiba: A témakör címe már létezik a kiválasztott tantárgyban!");
                    return;
                }
                if (_currentTopic != null)
                {
                    //update
                    _currentTopic.Title = trimmedTitle;
                    await _topicService.UpdateTopicAsync(_currentTopic);
                    resultTopic = _currentTopic;
                }
                else {
                    //create
                    var newTopic = new Topic
                    {
                        Title = trimmedTitle,
                        SubjectId = _parentSubjectId,
                    };
                    resultTopic = await _topicService.AddTopicAsync(newTopic);
                }
                TopicTitle = string.Empty;
                TopicSavedSuccessfully?.Invoke(resultTopic);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba a témakör mentésekor: {ex.Message}");
            }
        }
        private void ExecuteCancel(object parameter)
        {
            CancelRequested?.Invoke();
        }
    }
}
