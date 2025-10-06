using Core.Context;
using Core.Interfaces;
using Core.Repositories;
using Core.Services;
using MemoSphere.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using System.IO;

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
                }).ConfigureServices((context, services) =>
                {

                    services.AddSingleton<MainWindow>();

                    // Add DbContext
                    services.AddDbContext<MemoSphereDbContext>(options =>
                    {
                        var dbPath = Path.Combine(AppContext.BaseDirectory, "MemoSphere.db");
                        options.UseSqlite($"Data Source={dbPath}");
                    });



                    // Add Repositories
                    services.AddTransient<IUnitOfWork, UnitOfWork>();
                    services.AddTransient<IQuestionRepository, QuestionRepository>();
                    services.AddTransient<IAnswerRepository, AnswerRepository>();
                    services.AddTransient<ITopicRepository, TopicRepository>();
                    services.AddTransient<INoteRepository, NoteRepository>();
                    services.AddTransient<ISubjectRepository, SubjectRepository>();
                    services.AddTransient<INoteChunkRepository, NoteChunkRepository>();


                    services.AddTransient<IQuestionGeneratorService, GeminiService>(provider =>
                    {
                        var configuration = provider.GetRequiredService<IConfiguration>();
                        var apiKey = configuration["GeminiApi:ApiKey"];

                        if (string.IsNullOrEmpty(apiKey))
                        {
                            throw new InvalidOperationException("A Gemini API kulcs hiányzik a konfigurációból.");
                        }

                        return new GeminiService(apiKey);
                    });

                    // Add Services
                    services.AddTransient<IQuestionService, QuestionService>();
                    services.AddTransient<IAnswerService, AnswerService>();
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