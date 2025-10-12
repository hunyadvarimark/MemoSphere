using Core.Entities;
using Core.Interfaces.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using WPF.Utilities;

namespace WPF.ViewModels
{
    public class HierarchyViewModel : BaseViewModel
    {
        private readonly ISubjectService _subjectService;
        private readonly ITopicService _topicService;
        private readonly INoteService _noteService;
        private readonly SubjectDetailViewModel _subjectDetailVM;
        private readonly TopicDetailViewModel _topicDetailVM;



        public HierarchyViewModel(
            ISubjectService subjectService,
            ITopicService topicService,
            INoteService noteService,
            SubjectDetailViewModel subjectDetailViewModel,
            TopicDetailViewModel topicDetailViewModel)
        {
            _subjectService = subjectService;
            _topicService = topicService;
            _noteService = noteService;
            _subjectDetailVM = subjectDetailViewModel;
            _topicDetailVM = topicDetailViewModel;

            //------- COMMANDOK LÉTREHOZÁSA -------
            LoadSubjectsCommand = new AsyncCommand<object>(LoadSubjectsAsync);
            AddSubjectCommand = new AsyncCommand<object>(ExecuteAddSubjectAsync);
            EditSubjectCommand = new AsyncCommand<object>(ExecuteEditSubjectAsync, CanEditSubject);
            DeleteSubjectCommand = new AsyncCommand<object>(ExecuteDeleteSubjectAsync, CanDeleteSubject);
            LoadTopicsCommand = new AsyncCommand<object>(ExecuteLoadTopicsAsync, CanLoadTopics);
            AddTopicCommand = new RelayCommand(ExecuteAddTopic, CanAddTopic);
            EditTopicCommand = new RelayCommand(ExecuteEditTopic, CanEditTopic);
            DeleteTopicCommand = new AsyncCommand<object>(ExecuteDeleteTopicAsync, CanDeleteTopic);


            _subjectDetailVM.CancelRequested += OnSubjectCancelRequested;
            _subjectDetailVM.SubjectSavedSuccessfully += OnSubjectSavedSuccessfully;
            _topicDetailVM.TopicSavedSuccessfully += OnTopicSavedSuccessfully;
            _topicDetailVM.CancelRequested += OnTopicCancelRequested;

            // Adatbetöltés indítása
            //_ = LoadSubjectsAsync(null);
        }

        

        public SubjectDetailViewModel SubjectDetailVM => _subjectDetailVM;
        public TopicDetailViewModel TopicDetailVM => _topicDetailVM;

        private ObservableCollection<Subject> _subjects = new ObservableCollection<Subject>();
        public ObservableCollection<Subject> Subjects
        {
            get => _subjects;
            set { _subjects = value; OnPropertyChanged(); }
        }

        private Subject _selectedSubject;
        public Subject SelectedSubject
        {
            get => _selectedSubject;
            set
            {
                if (_selectedSubject != value)
                {
                    _selectedSubject = value;
                    OnPropertyChanged();

                    _ = LoadTopicsAsync();

                }
            }
        }

        private ObservableCollection<Topic> _topics = new ObservableCollection<Topic>();
        public ObservableCollection<Topic> Topics
        {
            get => _topics;
            set { _topics = value; OnPropertyChanged(); }
        }

        private Topic _selectedTopic;
        public Topic SelectedTopic
        {
            get => _selectedTopic;
            set
            {
                Debug.WriteLine($"SelectedTopic setter hívva (akár változott, akár nem): új={value?.Id ?? 0}");
                _selectedTopic = value;
                OnPropertyChanged();

                if (_selectedTopic != null)
                {
                    _ = LoadNotesAsync();
                }
                else
                {
                    Notes = new ObservableCollection<Note>();
                }
            }
        }

        private ObservableCollection<Note> _notes = new ObservableCollection<Note>();
        public ObservableCollection<Note> Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(); }
        }

        private Note _selectedNote;
        public Note SelectedNote
        {
            get => _selectedNote;
            set { _selectedNote = value; OnPropertyChanged(); }
        }

        private bool _isAddingSubject;
        public bool IsAddingSubject
        {
            get => _isAddingSubject;
            set { _isAddingSubject = value; OnPropertyChanged(); }
        }
        
        private bool _isAddingTopic;
        public bool IsAddingTopic
        {
            get => _isAddingTopic;
            set => SetProperty(ref _isAddingTopic, value);
        }

        // --- COMMANDS ---
        public ICommand LoadSubjectsCommand { get; }
        public ICommand AddSubjectCommand { get; }
        public ICommand EditSubjectCommand { get; }
        public ICommand DeleteSubjectCommand { get; }
        public ICommand AddTopicCommand { get; }
        public ICommand EditTopicCommand { get; }
        public ICommand DeleteTopicCommand { get; }
        public ICommand LoadTopicsCommand { get; }


        // --- METHODS ---
        private async Task LoadSubjectsAsync(object parameter)
        {
            try
            {
                var subjects = await _subjectService.GetAllSubjectsAsync();

                Subjects = new ObservableCollection<Subject>(subjects);

                SelectedSubject = Subjects.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba a tantárgyak betöltésekor: {ex.Message}");
            }
        }
        public async Task ReloadNotesAsync()
        {
            await LoadNotesAsync();
        }
        private async Task LoadTopicsAsync()
        {
            Topics.Clear();
            if (SelectedSubject == null)
            {
                SelectedTopic = null;
                return;
            }

            try
            {
                var topics = await _topicService.GetTopicBySubjectIdAsync(SelectedSubject.Id);
                foreach (var topic in topics.OrderBy(t => t.Title))
                {
                    Topics.Add(topic);
                }
                Debug.WriteLine($"Topics betöltve: Count={Topics.Count}, FirstOrDefault Id={Topics.FirstOrDefault()?.Id ?? 0}");
                SelectedTopic = Topics.FirstOrDefault();
                Debug.WriteLine($"SelectedTopic beállítva: Id={SelectedTopic?.Id ?? 0}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba a témakörök betöltésekor: {ex.Message}");
            }
        }
        private async Task LoadNotesAsync()
        {
            Notes.Clear();
            if (SelectedTopic == null)
            {
                return;
            }

            try
            {
                var notes = await _noteService.GetNotesByTopicIdAsync(SelectedTopic.Id);
                foreach (var note in notes)
                {
                    Notes.Add(note);
                }
                SelectedNote = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba a jegyzetek betöltésekor: {ex.Message}");
            }
        }
        public void UpdateNoteInList(Note savedNote)
        {
            if (savedNote == null) return;

            var existingNote = Notes.FirstOrDefault(n => n.Id == savedNote.Id);

            if (existingNote != null)
            {
                var index = Notes.IndexOf(existingNote);
                if(index >= 0)
                {
                    Notes[index] = savedNote;
                }
            }
        }
        private async Task ExecuteAddSubjectAsync(object parameter)
        {
            //1. Töröljük a SubjectDetailViewModel állapotát
            _subjectDetailVM.ResetState();
            //2. Megjelenítjük a SubjectDetailViewt
            IsAddingSubject = true;
            //3. Frissítsk a gomb állapotát
            _subjectDetailVM.RaiseCanExecuteChanged();

            return;
        }
        private async void OnSubjectSavedSuccessfully(Subject savedSubject)
        {
            IsAddingSubject = false;

            await LoadSubjectsAsync(null);

            // Opcionális: Kiválasztjuk az újonnan hozzáadott Tárgyat
            // SelectedSubject = Subjects.FirstOrDefault(s => s.Id == savedSubject.Id);
        }
        private void OnSubjectCancelRequested()
        {
            IsAddingSubject = false;
        }
        private async Task ExecuteEditSubjectAsync(object parameter)
        {
            var subjectToEdit = parameter as Subject;

            if(subjectToEdit != null)
            {
                SubjectDetailVM.LoadSubject(subjectToEdit);

                IsAddingSubject = true;
            }
        }
        private bool CanEditSubject(object parameter)
        {
            return SelectedSubject != null;
        }
        private async Task ExecuteDeleteSubjectAsync(object parameter)
        {
            var subjectToDelete = parameter as Subject;
            if(subjectToDelete == null)
            {
                return;
            }
            var result = System.Windows.MessageBox.Show(
                $"Biztosan törölni szeretnéd a(z) '{subjectToDelete.Title}' tantárgyat? Ez a művelet visszafordíthatatlan!",
                "Megerősítés",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            if(result == System.Windows.MessageBoxResult.No)
            {
                return;
            }
            else
            {
                try
                {
                    await _subjectService.DeleteSubjectAsync(subjectToDelete.Id);
                    
                    Subjects.Remove(subjectToDelete);
                    
                    if(SelectedSubject == subjectToDelete)
                    {
                        SelectedSubject = Subjects.FirstOrDefault();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hiba a tantárgy törlésekor: {ex.Message}");
                    MessageBox.Show($"Hiba történt a törlés során: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private bool CanDeleteSubject(object parameter)
        {
            return SelectedSubject != null;
        }
        private bool CanAddTopic(object parameter)
        {
            // Témakört csak akkor adhatunk hozzá, ha van kiválasztott tantárgy
            return SelectedSubject != null;
        }
        private void ExecuteAddTopic(object parameter)
        {
            if (SelectedSubject == null) return;

            // Állapot resetelése, beállítva a szülő Subject ID-t
            _topicDetailVM.ResetState(SelectedSubject.Id);

            // Modális ablak megjelenítése
            IsAddingTopic = true;
        }
        private bool CanEditTopic(object parameter)
        {
            return SelectedSubject != null;
        }
        private void ExecuteEditTopic(object parameter)
        {
            var topicToEdit = parameter as Topic;
            if(topicToEdit == null || SelectedSubject == null)
            {
                return;
            }
            _topicDetailVM.LoadTopic(topicToEdit);
            IsAddingTopic = true;
        }
        private bool CanDeleteTopic(object parameter)
        {
            return SelectedTopic != null;
        }
        private async Task ExecuteDeleteTopicAsync(object parameter)
        {
            var topicToDelete = parameter as Topic;
            if(topicToDelete == null)
            {
                return;
            }
            var result = MessageBox.Show(
                $"Biztosan törölni szeretnéd a(z) '{topicToDelete.Title}' témakört? Ez a művelet visszafordíthatatlan!",
                "Megerősítés",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if(result == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                try
                {
                    await _topicService.DeleteTopicAsync(topicToDelete.Id);
                    Topics.Remove(topicToDelete);
                    if(SelectedTopic == topicToDelete)
                    {
                        SelectedTopic = Topics.FirstOrDefault();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hiba a témakör törlésekor: {ex.Message}");
                    MessageBox.Show($"Hiba történt a törlés során: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private bool CanLoadTopics(object parameter)
        {
            return SelectedSubject != null;
        }
        private async Task ExecuteLoadTopicsAsync(object parameter)
        {
            await LoadTopicsAsync();
        }
        private void OnTopicCancelRequested()
        {
            IsAddingTopic = false;
        }
        private void OnTopicSavedSuccessfully(Topic savedTopic)
        {
            IsAddingTopic = false;

            var existingTopic = Topics.FirstOrDefault(t => t.Id == savedTopic.Id);

            if (existingTopic != null)
            {
                existingTopic.Title = savedTopic.Title;
            }
            else
            {
                Topics.Add(savedTopic);
                SelectedTopic = savedTopic;
            }
        }
    }
}