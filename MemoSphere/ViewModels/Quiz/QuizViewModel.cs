using Core.Interfaces.Services;
using Data.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WPF.Utilities;

namespace WPF.ViewModels.Quiz
{
    public class QuizViewModel : BaseViewModel
    {
        private readonly IQuizService _quizService;
        private readonly IQuestionService _questionService;
        private readonly DispatcherTimer _timer;
        private readonly int _quizDurationInSeconds = 300;
        private readonly int _requiredQuestionCount = 10;

        private bool _canStartQuiz = false;

        private ObservableCollection<QuizItemViewModel> _quizItems = new ObservableCollection<QuizItemViewModel>();
        private int _currentQuestionIndex = 0;
        private int _secondsRemaining;
        private bool _isQuizFinished = false;
        private int _correctAnswers;

        // --- Tulajdonságok a Binding-hez ---

        public ObservableCollection<QuizItemViewModel> QuizItems
        {
            get => _quizItems;
            set => SetProperty(ref _quizItems, value);
        }

        public bool CanStartQuiz
        {
            get => _canStartQuiz;
            private set => SetProperty(ref _canStartQuiz, value);
        }

        public QuizItemViewModel CurrentItem =>
            _quizItems.Count > _currentQuestionIndex ? _quizItems[_currentQuestionIndex] : null;

        public string StatusText =>
            IsQuizFinished ? "Kvíz Befejezve!" : $"Kérdés: {_currentQuestionIndex + 1}/{_quizItems.Count}";

        public string RemainingTimeText =>
            IsQuizFinished ? "Idő lejárt." : $"Idő: {TimeSpan.FromSeconds(_secondsRemaining):mm\\:ss}";

        public bool IsQuizFinished
        {
            get => _isQuizFinished;
            private set => SetProperty(ref _isQuizFinished, value);
        }

        public bool IsCurrentQuestionAnswered => CurrentItem?.IsAnswerSubmitted ?? false;

        public int CorrectAnswers
        {
            get => _correctAnswers;
            private set => SetProperty(ref _correctAnswers, value);
        }

        public int TotalQuestions => QuizItems.Count;

        public string ResultText => IsQuizFinished
            ? $"Eredmény: {CorrectAnswers}/{TotalQuestions} ({(TotalQuestions > 0 ? CorrectAnswers * 100.0 / TotalQuestions : 0):F1}%)"
            : string.Empty;

        private bool _isEvaluating;
        public bool IsEvaluating
        {
            get => _isEvaluating;
            private set => SetProperty(ref _isEvaluating, value);
        }

        // --- Command-ok ---
        public AsyncCommand<List<int>> LoadQuizCommand { get; }
        public AsyncCommand<object> SubmitAnswerCommand { get; }

        public ICommand NavigateNextCommand { get; }
        public ICommand RestartQuizCommand { get; }
        public RelayCommand CloseQuizCommand { get; }

        // Esemény a kvíz bezárásához
        public event Action CloseRequested;

        // --- Konstruktor ---

        public QuizViewModel(IQuizService quizService, IQuestionService questionService)
        {
            _quizService = quizService ?? throw new ArgumentNullException(nameof(quizService));
            _questionService = questionService ?? throw new ArgumentNullException(nameof(questionService));


            LoadQuizCommand = new AsyncCommand<List<int>>(LoadQuizAsync, CanLoadQuiz);
            SubmitAnswerCommand = new AsyncCommand<object>(SubmitAnswerAsync, CanSubmitAnswer);
            NavigateNextCommand = new RelayCommand(NavigateNext, CanNavigateNext);
            RestartQuizCommand = new RelayCommand(RestartQuiz, _ => IsQuizFinished);
            CloseQuizCommand = new RelayCommand(_ => CloseQuiz());

            _secondsRemaining = _quizDurationInSeconds;
            OnPropertyChanged(nameof(RemainingTimeText));

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }

        // --- Kvíz Folyamat Metódusok ---

        private async Task LoadQuizAsync(List<int> topicIds)
        {
            try
            {
                if (topicIds == null || !topicIds.Any())
                {
                    MessageBox.Show("Válassz legalább egy témakört a kvízhez!");
                    return;
                }

                // Kérünk 10 kérdést a kiválasztott témakörökből
                var questions = await _quizService.GetRandomQuestionsForQuizAsync(topicIds, _requiredQuestionCount);

                if (questions.Count < _requiredQuestionCount)
                {
                    MessageBox.Show("Nincs elég elérhető kérdés a kiválasztott témakörökből.");
                    return;
                }

                QuizItems = new ObservableCollection<QuizItemViewModel>(
                    questions.Select(q => new QuizItemViewModel(q)).Where(vm => vm != null)  // Null filter
                );

                _currentQuestionIndex = 0;
                _secondsRemaining = _quizDurationInSeconds;
                IsQuizFinished = false;
                CorrectAnswers = 0;

                // Kvíz indítása
                _timer.Start();

                OnPropertyChanged(nameof(CurrentItem));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(RemainingTimeText));
                OnPropertyChanged(nameof(TotalQuestions));
                OnPropertyChanged(nameof(ResultText));

                RaiseCommandsCanExecuteChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a kvíz betöltésekor: {ex.Message}");
                _timer.Stop();
                IsQuizFinished = true;
            }
        }

        private bool CanLoadQuiz(List<int> topicIds) => !IsQuizFinished && _canStartQuiz;

        private bool CanSubmitAnswer(object parameter) =>
            !IsQuizFinished &&
            !IsEvaluating &&
            CurrentItem != null &&
            !IsCurrentQuestionAnswered &&
            (CurrentItem.IsShortAnswer
                ? !string.IsNullOrWhiteSpace(CurrentItem.UserAnswerText)
                : CurrentItem.SelectedAnswer != null);

        private bool CanNavigateNext(object parameter) =>
            !IsQuizFinished && IsCurrentQuestionAnswered;

        private async Task SubmitAnswerAsync(object parameter)
        {
            if (CurrentItem == null || IsQuizFinished) return;

            if (CurrentItem.IsShortAnswer)
            {
                IsEvaluating = true;
                try
                {
                    bool isCorrect = await _questionService.EvaluateUserShortAnswerAsync(
                        CurrentItem.Question.Id,
                        CurrentItem.UserAnswerText
                    );

                    CurrentItem.SetLLMEvaluationResult(isCorrect);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Hiba a válasz kiértékelése során: {ex.Message}");
                    CurrentItem.SetLLMEvaluationResult(false);
                }
                finally
                {
                    IsEvaluating = false;
                }
            }

            CurrentItem.IsAnswerSubmitted = true;
            OnPropertyChanged(nameof(IsCurrentQuestionAnswered));
            RaiseCommandsCanExecuteChanged();
        }


        private void NavigateNext(object parameter)
        {
            if (_currentQuestionIndex < _quizItems.Count - 1)
            {
                _currentQuestionIndex++;
                OnPropertyChanged(nameof(CurrentItem));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsCurrentQuestionAnswered));

                RaiseCommandsCanExecuteChanged();
            }
            else
            {
                EndQuiz();
            }
        }

        private void RestartQuiz(object parameter)
        {
            _timer.Stop();
            QuizItems.Clear();
            _currentQuestionIndex = 0;
            _secondsRemaining = _quizDurationInSeconds;
            IsQuizFinished = false;
            CorrectAnswers = 0;

            OnPropertyChanged(nameof(CurrentItem));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(RemainingTimeText));
            OnPropertyChanged(nameof(ResultText));
            OnPropertyChanged(nameof(TotalQuestions));
            OnPropertyChanged(nameof(IsCurrentQuestionAnswered));

            RaiseCommandsCanExecuteChanged();
        }

        // Segédmetódus a Commandok állapotának frissítéséhez
        private void RaiseCommandsCanExecuteChanged()
        {
            SubmitAnswerCommand.RaiseCanExecuteChanged();
            ((RelayCommand)NavigateNextCommand).RaiseCanExecuteChanged();
            ((RelayCommand)RestartQuizCommand).RaiseCanExecuteChanged();
            LoadQuizCommand.RaiseCanExecuteChanged();
        }

        // --- Timer és Kiértékelés ---

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_secondsRemaining > 0)
                {
                    _secondsRemaining--;
                    OnPropertyChanged(nameof(RemainingTimeText));
                }
                else
                {
                    EndQuiz();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Timer hiba: {ex}");
                _timer.Stop();
            }
        }

        private void EndQuiz()
        {
            _timer.Stop();

            // Automatikusan beküldjük a még nem beküldött válaszokat
            foreach (var item in QuizItems.Where(i => !i.IsAnswerSubmitted))
            {
                item.IsAnswerSubmitted = true;
            }

            IsQuizFinished = true;

            // Eredmény számítás
            CorrectAnswers = QuizItems.Count(item => item.IsCorrect);

            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(RemainingTimeText));
            OnPropertyChanged(nameof(ResultText));
            OnPropertyChanged(nameof(CorrectAnswers));
            OnPropertyChanged(nameof(TotalQuestions));
            OnPropertyChanged(nameof(IsCurrentQuestionAnswered));

            RaiseCommandsCanExecuteChanged();
        }

        private void CloseQuiz()
        {
            _timer.Stop();
            CloseRequested?.Invoke();
        }

        public async Task ValidateTopicsForQuizAsync(List<int> topicIds)
        {
            System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine("🔍 QuizVM.ValidateTopicsForQuizAsync STARTED");
            System.Diagnostics.Debug.WriteLine($"📥 TopicIds: {string.Join(", ", topicIds ?? new List<int>())}");

            if (topicIds == null || !topicIds.Any())
            {
                System.Diagnostics.Debug.WriteLine("⚠️ No topic IDs provided");
                CanStartQuiz = false;
                System.Diagnostics.Debug.WriteLine($"✅ CanStartQuiz set to: {CanStartQuiz}");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
            }
            else
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("🔍 Calling _quizService.GetQuestionCountForTopicsAsync...");

                    var questionCount = await _quizService.GetQuestionCountForTopicsAsync(topicIds);

                    System.Diagnostics.Debug.WriteLine($"📊 Question count returned: {questionCount}");
                    System.Diagnostics.Debug.WriteLine($"📊 Required count: {_requiredQuestionCount}");

                    var oldValue = CanStartQuiz;
                    CanStartQuiz = questionCount >= _requiredQuestionCount;

                    System.Diagnostics.Debug.WriteLine($"✅ CanStartQuiz: {oldValue} → {CanStartQuiz}");
                    System.Diagnostics.Debug.WriteLine($"🔔 Raising LoadQuizCommand.CanExecuteChanged");

                    LoadQuizCommand.RaiseCanExecuteChanged();

                    System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ ERROR in ValidateTopicsForQuizAsync: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                    CanStartQuiz = false;
                    System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                }
            }
        }
    }
}