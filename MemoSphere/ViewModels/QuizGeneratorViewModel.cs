using System.Windows.Input;
using Core.Enums;
using Core.Interfaces.Services;
using WPF.Utilities;


namespace WPF.ViewModels
{

    public class QuizGeneratorViewModel : BaseViewModel
    {
        private readonly IQuestionService _questionService;

        // 1. Command Property: Felel a XAML-beli gombkattintásért
        public ICommand GenerateQuizCommand { get; }

        // 2. Status Message Property: Visszajelzés a felhasználónak
        private string _statusMessage = "Kész a kérdésgenerálásra.";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        // 3. IsGenerating Property: Megakadályozza a többszöri kattintást generálás közben
        private bool _isGenerating;
        public bool IsGenerating
        {
            get => _isGenerating;
            set
            {
                _isGenerating = value;
                OnPropertyChanged();
                // A gombok CanExecute állapotának frissítése, ha a státusz változik
                // Bár az AsyncCommand kezeli, ez a XAML kötéshez is jó, ha használnál IsEnabled bindet
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public QuizGeneratorViewModel(IQuestionService questionService)
        {
            _questionService = questionService;

            // Initializálás: A command generikus paramétere a QuestionType, amit a XAML-ből kap
            GenerateQuizCommand = new AsyncCommand<QuestionType>(
                execute: GenerateQuizAsync,
                canExecute: (type) => !IsGenerating // Csak akkor futtatható, ha IsGenerating=false
            );
        }

        private async Task GenerateQuizAsync(QuestionType selectedType)
        {
            // Az IsGenerating property-t az AsyncCommand már kezeli, de biztonsági okból:
            if (IsGenerating) return;

            IsGenerating = true;
            StatusMessage = $"Kérdésgenerálás indítása: '{selectedType}' típusú kvízhez (Note ID: 1)...";

            try
            {
                // A Note ID fixen 1, mivel még nincs jegyzetbevitel
                bool success = await _questionService.GenerateAndSaveQuestionsAsync(1, selectedType);

                if (success)
                {
                    StatusMessage = $"Sikeresen generálva és mentve: '{selectedType}' (ellenőrizd az adatbázist).";
                }
                else
                {
                    StatusMessage = $"Generálás sikertelen: Nincs jegyzet tartalom a Note ID 1-hez, vagy az API nem válaszolt.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Hiba történt a generálás közben: {ex.Message}";
            }
            finally
            {
                IsGenerating = false;
            }
        }
    }
}