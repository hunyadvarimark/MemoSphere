using Core.Interfaces.Services;
using Core.Services;
using Data.Context;
using Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Windows;
using WPF.ViewModels;
using WPF.ViewModels.Notes;
using WPF.ViewModels.Questions;
using WPF.ViewModels.Quiz;
using WPF.ViewModels.Subjects;
using WPF.ViewModels.Topics;

namespace MemoSphere.WPF
{
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.AddUserSecrets<App>();
                })
                .ConfigureServices((context, services) =>
                {
                    // MainWindow
                    services.AddSingleton<MainWindow>();

                    // DbContext
                    services.AddDbContext<MemoSphereDbContext>(options =>
                    {
                        var dbPath = Path.Combine(AppContext.BaseDirectory, "MemoSphere.db");
                        options.UseSqlite($"Data Source={dbPath}");
                    });

                    // Gemini Service
                    //services.AddTransient<IQuestionGeneratorService, GeminiService>(provider =>
                    //{
                    //    var configuration = provider.GetRequiredService<IConfiguration>();
                    //    var apiKey = configuration["GeminiApi:ApiKey"];

                    //    if (string.IsNullOrEmpty(apiKey))
                    //    {
                    //        throw new InvalidOperationException("A Gemini API kulcs hiányzik a konfigurációból.");
                    //    }

                    //    return new GeminiService(apiKey);
                    //});
                    services.AddTransient<IQuestionGeneratorService, OllamaService>();

                    // Core Services
                    services.AddTransient<IUnitOfWork, UnitOfWork>();
                    services.AddTransient<IQuestionService, QuestionService>();
                    services.AddTransient<IAnswerService, AnswerService>();
                    services.AddTransient<INoteService, NoteService>();
                    services.AddTransient<ITopicService, TopicService>();
                    services.AddTransient<ISubjectService, SubjectService>();
                    services.AddTransient<IQuizService, QuizService>();

                    // ViewModels - SORREND FONTOS!

                    // 1. List ViewModels (nincs függőség más ViewModelekre)
                    services.AddSingleton<SubjectListViewModel>();
                    services.AddSingleton<TopicListViewModel>();
                    services.AddSingleton<NoteListViewModel>();
                    services.AddSingleton<QuestionListViewModel>();

                    // 2. Detail ViewModels
                    services.AddSingleton<SubjectDetailViewModel>();
                    services.AddSingleton<TopicDetailViewModel>();
                    services.AddSingleton<NoteDetailViewModel>();
                    services.AddSingleton<QuestionDetailViewModel>();

                    // 3. Quiz ViewModel
                    services.AddSingleton<QuizViewModel>();

                    // 4. Coordinators és Handlers (függenek más ViewModelektől)
                    services.AddSingleton<HierarchyCoordinator>(provider =>
                    {
                        var subjectsVM = provider.GetRequiredService<SubjectListViewModel>();
                        var topicsVM = provider.GetRequiredService<TopicListViewModel>();
                        var notesVM = provider.GetRequiredService<NoteListViewModel>();
                        var questionsVM = provider.GetRequiredService<QuestionListViewModel>();
                        var noteDetailVM = provider.GetRequiredService<NoteDetailViewModel>();

                        return new HierarchyCoordinator(subjectsVM, topicsVM, notesVM, questionsVM, noteDetailVM);
                    });

                    services.AddSingleton<CrudOperationHandler>(provider =>
                    {
                        var subjectService = provider.GetRequiredService<ISubjectService>();
                        var topicService = provider.GetRequiredService<ITopicService>();
                        var noteService = provider.GetRequiredService<INoteService>();
                        var subjectsVM = provider.GetRequiredService<SubjectListViewModel>();
                        var topicsVM = provider.GetRequiredService<TopicListViewModel>();
                        var notesVM = provider.GetRequiredService<NoteListViewModel>();

                        return new CrudOperationHandler(subjectService, topicService, noteService, subjectsVM, topicsVM, notesVM);
                    });

                    // 5. MainViewModel UTOLJÁRA (függ minden ViewModeltől, Coordinator-tól és Handler-től)
                    services.AddSingleton<MainViewModel>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            using (var scope = _host.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MemoSphereDbContext>();
                await dbContext.Database.MigrateAsync();
            }

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            base.OnExit(e);
        }
    }
}