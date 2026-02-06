using Core.Entities;
using Core.Interfaces.Services;
using Data.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IActiveLearningService _activeLearningService;
        private readonly DispatcherTimer _timer;

        // --- Beállítások ---
        private const int SecondsPerStandardQuestion = 60; // 1 perc kérdésenként
        private const int SecondsPerShortAnswerQuestion = 180; // 3 perc a kifejtősnél
        private const int MaxQuestionsPerNoteQuiz = 10;

        private readonly int _requiredQuestionCount = 10;
        private readonly int _minRequiredQuestionCount = 3;

        private bool _canStartQuiz = false;

        private ObservableCollection<QuizItemViewModel> _quizItems = new ObservableCollection<QuizItemViewModel>();
        private int _currentQuestionIndex = 0;
        private int _secondsRemaining;
        private bool _isQuizFinished = false;
        private int _correctAnswers;

        // --- Tulajdonságok ---

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
        public AsyncCommand<int> LoadQuizFromNoteCommand { get; }
        public AsyncCommand<object> SubmitAnswerCommand { get; }

        public ICommand NavigateNextCommand { get; }
        public ICommand RestartQuizCommand { get; }
        public RelayCommand CloseQuizCommand { get; }

        public event Action CloseRequested;

        // --- Konstruktor ---

        public QuizViewModel(
            IQuizService quizService,
            IQuestionService questionService,
            IActiveLearningService activeLearningService)
        {
            _quizService = quizService ?? throw new ArgumentNullException(nameof(quizService));
            _questionService = questionService ?? throw new ArgumentNullException(nameof(questionService));
            _activeLearningService = activeLearningService ?? throw new ArgumentNullException(nameof(activeLearningService));

            LoadQuizCommand = new AsyncCommand<List<int>>(LoadQuizAsync, CanLoadQuiz);
            SubmitAnswerCommand = new AsyncCommand<object>(SubmitAnswerAsync, CanSubmitAnswer);
            NavigateNextCommand = new RelayCommand(NavigateNext, CanNavigateNext);
            RestartQuizCommand = new RelayCommand(RestartQuiz, _ => IsQuizFinished);
            CloseQuizCommand = new RelayCommand(_ => CloseQuiz());
            LoadQuizFromNoteCommand = new AsyncCommand<int>(LoadQuizFromNoteAsync, CanLoadQuizFromNote);

            _secondsRemaining = 0;
            OnPropertyChanged(nameof(RemainingTimeText));

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }

        // --- Kvíz Folyamat ---

        private async Task LoadQuizAsync(List<int> topicIds)
        {
            try
            {
                if (topicIds == null || !topicIds.Any())
                {
                    MessageBox.Show("Válassz legalább egy témakört a kvízhez!");
                    return;
                }

                var availableCount = await _quizService.GetQuestionCountForTopicsAsync(topicIds);
                int questionsToLoad = Math.Min(_requiredQuestionCount, availableCount);

                if (availableCount < _minRequiredQuestionCount)
                {
                    MessageBox.Show($"Túl kevés kérdés van ({availableCount}). Legalább {_minRequiredQuestionCount} szükséges a kvízhez.");
                    return;
                }

                var questions = await _quizService.GetRandomQuestionsForQuizAsync(topicIds, questionsToLoad);

                if (questions == null || !questions.Any())
                {
                    MessageBox.Show("Nem sikerült kérdéseket betölteni a kvízhez.");
                    return;
                }

                QuizItems = new ObservableCollection<QuizItemViewModel>(
                    questions.Select(q => new QuizItemViewModel(q)).Where(vm => vm != null)
                );

                StartQuizLogic();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a kvíz betöltésekor: {ex.Message}");
                _timer.Stop();
                IsQuizFinished = true;
            }
        }

        private async Task LoadQuizFromNoteAsync(int noteId)
        {
            try
            {
                var allQuestions = (await _questionService.GetQuestionsForNoteAsync(noteId)).ToList();

                if (allQuestions == null || !allQuestions.Any())
                {
                    MessageBox.Show("Ehhez a jegyzethez még nem tartoznak kérdések.", "Nincs kérdés", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var random = new Random();
                var selectedQuestions = allQuestions
                    .OrderBy(x => random.Next())
                    .Take(MaxQuestionsPerNoteQuiz)
                    .ToList();

                QuizItems = new ObservableCollection<QuizItemViewModel>(
                    selectedQuestions.Select(q => new QuizItemViewModel(q)).Where(vm => vm != null)
                );

                StartQuizLogic();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a jegyzet-kvíz betöltésekor: {ex.Message}");
                _timer.Stop();
                IsQuizFinished = true;
            }
        }

        private void StartQuizLogic()
        {
            _currentQuestionIndex = 0;

            _secondsRemaining = QuizItems.Sum(item =>
                item.IsShortAnswer ? SecondsPerShortAnswerQuestion : SecondsPerStandardQuestion);

            IsQuizFinished = false;
            CorrectAnswers = 0;

            _timer.Start();

            OnPropertyChanged(nameof(CurrentItem));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(RemainingTimeText));
            OnPropertyChanged(nameof(TotalQuestions));
            OnPropertyChanged(nameof(ResultText));

            RaiseCommandsCanExecuteChanged();
        }

        private bool CanLoadQuiz(List<int> topicIds) => !IsQuizFinished && _canStartQuiz;
        private bool CanLoadQuizFromNote(int noteId) => noteId > 0 && !IsQuizFinished;

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

            bool calculatedIsCorrect = false;

            if (CurrentItem.IsShortAnswer)
            {
                IsEvaluating = true;
                try
                {
                    var evalResult = await _questionService.EvaluateUserShortAnswerAsync(
                        CurrentItem.Question.Id,
                        CurrentItem.UserAnswerText
                    );

                    calculatedIsCorrect = evalResult.IsCorrect;
                    string explanation = evalResult.Explanation;

                    CurrentItem.SetLLMEvaluationResult(calculatedIsCorrect, explanation);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Hiba a válasz kiértékelése során: {ex.Message}");
                    CurrentItem.SetLLMEvaluationResult(false, "Hiba történt a kiértékelés során.");
                    calculatedIsCorrect = false;
                }
                finally
                {
                    IsEvaluating = false;
                }
            }
            else
            {
                calculatedIsCorrect = CurrentItem.SelectedAnswer?.IsCorrect ?? false;
            }

            CurrentItem.IsAnswerSubmitted = true;

            // Statisztika rögzítése
            try
            {
                await _questionService.RecordAnswerAsync(
                    questionId: CurrentItem.Question.Id,
                    isCorrect: calculatedIsCorrect
                );
                Debug.WriteLine($"✅ Statisztika rögzítve: QuestionId={CurrentItem.Question.Id}, IsCorrect={calculatedIsCorrect}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Hiba a statisztika rögzítésekor: {ex.Message}");
            }

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

            foreach (var item in QuizItems)
            {
                item.Reset();
            }

            foreach (var item in QuizItems)
            {
                var random = new Random();
                var shuffled = item.Question.Answers.OrderBy(a => random.Next()).ToList();
                item.AnswerOptions.Clear();
                foreach (var answer in shuffled)
                {
                    item.AnswerOptions.Add(answer);
                }
            }

            StartQuizLogic();
        }

        private void RaiseCommandsCanExecuteChanged()
        {
            SubmitAnswerCommand.RaiseCanExecuteChanged();
            ((RelayCommand)NavigateNextCommand).RaiseCanExecuteChanged();
            ((RelayCommand)RestartQuizCommand).RaiseCanExecuteChanged();
            LoadQuizCommand.RaiseCanExecuteChanged();
        }

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

        private async void EndQuiz()
        {
            _timer.Stop();

            foreach (var item in QuizItems.Where(i => !i.IsAnswerSubmitted))
            {
                item.IsAnswerSubmitted = true;
            }

            IsQuizFinished = true;
            CorrectAnswers = QuizItems.Count(item => item.IsCorrect);

            await UpdateAllTopicsProgressAsync();

            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(RemainingTimeText));
            OnPropertyChanged(nameof(ResultText));
            OnPropertyChanged(nameof(CorrectAnswers));
            OnPropertyChanged(nameof(TotalQuestions));
            OnPropertyChanged(nameof(IsCurrentQuestionAnswered));

            RaiseCommandsCanExecuteChanged();
        }

        private async Task UpdateAllTopicsProgressAsync()
        {
            try
            {
                // Csoportosítjuk a kérdéseket topic szerint
                var topicGroups = QuizItems
                    .Where(item => item.IsAnswerSubmitted) // Csak a megválaszolt kérdések
                    .GroupBy(item => item.Question.TopicId)
                    .ToList();

                Debug.WriteLine($"📊 Frissítés {topicGroups.Count} topic-ra");

                foreach (var topicGroup in topicGroups)
                {
                    int topicId = topicGroup.Key;

                    // Csak a helyes válaszokat számoljuk a haladáshoz
                    var correctAnswers = topicGroup.Count(item => item.IsCorrect);

                    Debug.WriteLine($"📚 Topic {topicId}: {correctAnswers} helyes válasz {topicGroup.Count()}-ból");

                    // Minden helyes válaszért frissítjük a haladást
                    for (int i = 0; i < correctAnswers; i++)
                    {
                        try
                        {
                            await _activeLearningService.UpdateProgressAsync(topicId, isCorrect: true);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"⚠️ Hiba a topic {topicId} haladásának frissítésekor: {ex.Message}");
                        }
                    }

                    Debug.WriteLine($"✅ Topic {topicId} haladása frissítve!");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Hiba az aktív tanulási haladás frissítésekor: {ex.Message}");
            }
        }

        private void CloseQuiz()
        {
            System.Diagnostics.Debug.WriteLine("🚪 CloseQuiz called");
            ResetState();
            CloseRequested?.Invoke();
        }

        public void ResetState()
        {
            _timer?.Stop();
            _quizItems.Clear();
            _currentQuestionIndex = 0;
            _secondsRemaining = 0;
            _isQuizFinished = false;
            _correctAnswers = 0;
            _isEvaluating = false;

            OnPropertyChanged(nameof(QuizItems));
            OnPropertyChanged(nameof(CurrentItem));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(RemainingTimeText));
            OnPropertyChanged(nameof(IsQuizFinished));
            OnPropertyChanged(nameof(IsCurrentQuestionAnswered));
            OnPropertyChanged(nameof(ResultText));

            RaiseCommandsCanExecuteChanged();
        }

        public async Task ValidateTopicsForQuizAsync(List<int> topicIds)
        {
            System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine("🔍 QuizVM.ValidateTopicsForQuizAsync STARTED");

            if (topicIds == null || !topicIds.Any())
            {
                CanStartQuiz = false;
            }
            else
            {
                try
                {
                    var questionCount = await _quizService.GetQuestionCountForTopicsAsync(topicIds);
                    CanStartQuiz = questionCount >= _minRequiredQuestionCount;
                    LoadQuizCommand.RaiseCanExecuteChanged();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ ERROR in ValidateTopicsForQuizAsync: {ex.Message}");
                    CanStartQuiz = false;
                }
            }
        }
    }
}