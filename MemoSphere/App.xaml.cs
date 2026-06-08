using Core.Interfaces.Services;
using Data.Context;
using Data.Services;
using MemoSphere.WPF.Views;
using MemoSphere.WPF.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using WPF.ViewModels;
using WPF.ViewModels.Dashboard;
using WPF.ViewModels.Notes;
using WPF.ViewModels.Questions;
using WPF.ViewModels.Quiz;
using WPF.ViewModels.Subjects;
using WPF.ViewModels.Topics;

namespace MemoSphere.WPF
{
    public partial class App : Application
    {
        public readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;

                    services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5000/") });

                    services.AddSingleton<IAuthService, ClientAuthService>();
                    services.AddSingleton<INoteService, ClientNoteService>();
                    services.AddSingleton<ISubjectService, ClientSubjectService>();
                    services.AddSingleton<ITopicService, ClientTopicService>();
                    services.AddSingleton<IQuestionService, ClientQuestionService>();
                    services.AddSingleton<INoteShareService, ClientNoteShareService>();
                    services.AddSingleton<IDocumentImportService, ClientDocumentImportService>();

                    services.AddSingleton<ClientQuizService>();
                    services.AddSingleton<IQuizService>(sp => sp.GetRequiredService<ClientQuizService>());
                    services.AddSingleton<IActiveLearningService>(sp => sp.GetRequiredService<ClientQuizService>());

                    // ====================================================================
                    // VIEWMODEL-EK ÉS KOORDINÁTOROK (Hozzájuk sem kellett nyúlni!)
                    // Automatikusan az új kliens szervizeket kapják meg interfészen keresztül.
                    // ====================================================================
                    services.AddSingleton<MainWindow>();
                    services.AddTransient<LoginWindow>(sp =>
                    {
                        var authService = sp.GetRequiredService<IAuthService>();
                        var mainWindow = sp.GetRequiredService<MainWindow>();
                        return new LoginWindow(authService, mainWindow);
                    });

                    services.AddSingleton<SubjectListViewModel>();
                    services.AddSingleton<TopicListViewModel>();
                    services.AddSingleton<NoteListViewModel>();
                    services.AddSingleton<QuestionListViewModel>();
                    services.AddSingleton<SubjectDetailViewModel>();
                    services.AddSingleton<TopicDetailViewModel>();
                    services.AddSingleton<NoteDetailViewModel>();
                    services.AddSingleton<QuestionDetailViewModel>();
                    services.AddSingleton<QuizViewModel>();
                    services.AddSingleton<DashboardViewModel>();
                    services.AddSingleton<QuizTopicSelectionViewModel>();

                    services.AddSingleton<HierarchyCoordinator>(provider =>
                    {
                        var subjectsVM = provider.GetRequiredService<SubjectListViewModel>();
                        var topicsVM = provider.GetRequiredService<TopicListViewModel>();
                        var notesVM = provider.GetRequiredService<NoteListViewModel>();
                        var questionsVM = provider.GetRequiredService<QuestionListViewModel>();
                        var noteDetailVM = provider.GetRequiredService<NoteDetailViewModel>();
                        var quizVM = provider.GetRequiredService<QuizViewModel>();

                        return new HierarchyCoordinator(subjectsVM, topicsVM, notesVM, questionsVM, noteDetailVM, quizVM);
                    });

                    services.AddSingleton<CrudOperationHandler>(provider =>
                    {
                        var subjectService = provider.GetRequiredService<ISubjectService>();
                        var topicService = provider.GetRequiredService<ITopicService>();
                        var noteService = provider.GetRequiredService<INoteService>();
                        var subjectsVM = provider.GetRequiredService<SubjectListViewModel>();
                        var topicsVM = provider.GetRequiredService<TopicListViewModel>();
                        var notesVM = provider.GetRequiredService<NoteListViewModel>();
                        var questionService = provider.GetRequiredService<IQuestionService>();

                        return new CrudOperationHandler(subjectService, topicService, noteService, questionService, subjectsVM, topicsVM, notesVM);
                    });

                    services.AddSingleton<MainViewModel>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();
            Debug.WriteLine("=== Alkalmazás indítása ===");

            try
            {
                var authService = _host.Services.GetRequiredService<IAuthService>();
                var isAuthenticated = await authService.IsAuthenticatedAsync();
                Debug.WriteLine($"👤 IsAuthenticated: {isAuthenticated}");

                if (isAuthenticated)
                {
                    var currentUser = authService.GetCurrentUserEmail();
                    Debug.WriteLine($"👤 Bejelentkezett felhasználó: {currentUser}");

                    try
                    {
                        var activeLearningService = _host.Services.GetRequiredService<IActiveLearningService>();
                        Debug.WriteLine("🔄 Streak-ek ellenőrzése indítása...");
                        await activeLearningService.CheckStreaksOnLoginAsync();
                        Debug.WriteLine("✅ Streak-ek ellenőrzése befejeződött.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Hiba a streak-ek ellenőrzésekor: {ex.Message}");
                    }

                    try
                    {
                        Debug.WriteLine("🖥️ MainWindow inicializálása indítása...");
                        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                        Debug.WriteLine("🔄 LoadDataAsync hívása...");
                        await mainWindow.LoadDataAsync();
                        Debug.WriteLine("✅ LoadDataAsync befejezve");
                        mainWindow.Show();
                        Debug.WriteLine("✅ MainWindow megjelenítve");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Hiba a MainWindow betöltése során: {ex.Message}");
                        MessageBox.Show($"Hiba az alkalmazás indításakor (MainWindow):\n\n{ex.Message}", "Kritikus hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                        Shutdown();
                    }
                }
                else
                {
                    Debug.WriteLine("🚫 Nincs érvényes session - LoginWindow megnyitása");
                    try
                    {
                        await authService.SignOutAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ SignOut hiba: {ex.Message}");
                    }

                    try
                    {
                        var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
                        loginWindow.Show();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Hiba a LoginWindow megnyitásakor: {ex.Message}");
                        MessageBox.Show($"Hiba az alkalmazás indításakor (LoginWindow):\n\n{ex.Message}", "Kritikus hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                        Shutdown();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ HIBA az indítás során: {ex.Message}");
                MessageBox.Show($"Hiba az alkalmazás indításakor:\n\n{ex.Message}", "Kritikus hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            base.OnExit(e);
        }
    }
}