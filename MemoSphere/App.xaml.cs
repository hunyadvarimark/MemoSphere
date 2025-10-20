using Core.Interfaces.Services;
using Core.Services;
using Data.Context;
using Data.Services;
using MemoSphere.WPF.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Linq;
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
                    var configuration = context.Configuration;

                    // SUPABASE CLIENT INICIALIZÁLÁS
                    var supabaseUrl = configuration["Supabase:Url"];
                    var supabaseAnonKey = configuration["Supabase:AnonKey"];
                    if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseAnonKey))
                    {
                        throw new InvalidOperationException("Supabase URL vagy Anon Key hiányzik a konfigurációból.");
                    }

                    var supabaseOptions = new Supabase.SupabaseOptions
                    {
                        AutoConnectRealtime = false,
                        AutoRefreshToken = true,
                    };

                    var supabaseClient = new Supabase.Client(supabaseUrl, supabaseAnonKey, supabaseOptions);
                    services.AddSingleton(supabaseClient);

                    // Windows
                    services.AddSingleton<MainWindow>();
                    services.AddTransient<LoginWindow>(sp =>
                    {
                        var authService = sp.GetRequiredService<IAuthService>();
                        var mainWindow = sp.GetRequiredService<MainWindow>();
                        return new LoginWindow(authService, mainWindow);
                    });

                    // DbContext - PostgreSQL
                    var connectionString = configuration.GetConnectionString("Supabase") ?? configuration["Supabase:ConnectionString"];
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        throw new InvalidOperationException("Supabase connection string hiányzik.");
                    }
                    services.AddDbContextFactory<MemoSphereDbContext>(options =>
                    {
                        options.UseNpgsql(connectionString);
                    });

                    // Gemini Service
                    services.AddTransient<IQuestionGeneratorService, GeminiService>(provider =>
                    {
                        var config = provider.GetRequiredService<IConfiguration>();
                        var apiKey = config["GeminiApi:ApiKey"];

                        if (string.IsNullOrEmpty(apiKey))
                        {
                            throw new InvalidOperationException("A Gemini API kulcs hiányzik a konfigurációból.");
                        }

                        return new GeminiService(apiKey);
                    });

                    // Core Services
                    services.AddTransient<IUnitOfWork, UnitOfWork>();
                    services.AddTransient<IQuestionService, QuestionService>();
                    services.AddTransient<IAnswerService, AnswerService>();
                    services.AddTransient<INoteService, NoteService>();
                    services.AddTransient<ITopicService, TopicService>();
                    services.AddTransient<ISubjectService, SubjectService>();
                    services.AddTransient<IQuizService, QuizService>();
                    services.AddTransient<IAuthService, AuthService>();

                    // ViewModels
                    services.AddSingleton<SubjectListViewModel>();
                    services.AddSingleton<TopicListViewModel>();
                    services.AddSingleton<NoteListViewModel>();
                    services.AddSingleton<QuestionListViewModel>();
                    services.AddSingleton<SubjectDetailViewModel>();
                    services.AddSingleton<TopicDetailViewModel>();
                    services.AddSingleton<NoteDetailViewModel>();
                    services.AddSingleton<QuestionDetailViewModel>();
                    services.AddSingleton<QuizViewModel>();

                    // Coordinators és Handlers
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

                        return new CrudOperationHandler(subjectService, topicService, noteService, subjectsVM, topicsVM, notesVM);
                    });

                    services.AddSingleton<MainViewModel>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            Debug.WriteLine("=== Alkalmazás indítása ===");

            if (e.Args.Length > 0)
            {
                var argument = e.Args[0];
                Debug.WriteLine($"📧 Startup argument kapva: {argument}");

                if (argument.StartsWith("memosphere://auth/callback"))
                {
                    Debug.WriteLine("✉️ Email confirmation callback észlelve!");
                    await HandleEmailConfirmationCallback(argument);
                    return; // NE folytassuk a normál indítást
                }
            }
            try
            {
                // Adatbázis migráció
                using (var scope = _host.Services.CreateScope())
                {
                    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MemoSphereDbContext>>();
                    using var dbContext = factory.CreateDbContext();
                    await dbContext.Database.MigrateAsync();
                    Debug.WriteLine("Adatbázis migráció befejezve");
                }

                // Supabase inicializálás
                var supabaseClient = _host.Services.GetRequiredService<Supabase.Client>();
                await supabaseClient.InitializeAsync();
                Debug.WriteLine("Supabase inicializálva");

                var authService = _host.Services.GetRequiredService<IAuthService>();
                var isAuthenticated = await authService.IsAuthenticatedAsync();

                Debug.WriteLine($"IsAuthenticated: {isAuthenticated}");

                if (isAuthenticated)
                {
                    var currentUser = authService.GetCurrentUserEmail();
                    Debug.WriteLine($"Bejelentkezett felhasználó: {currentUser}");

                    // MainWindow betöltése
                    var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                    await mainWindow.LoadDataAsync();
                    mainWindow.Show();
                }
                else
                {
                    Debug.WriteLine("Nincs érvényes session - LoginWindow megnyitása");

                    try
                    {
                        await authService.SignOutAsync();
                    }
                    catch { }

                    // LoginWindow megnyitása
                    var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
                    loginWindow.Show();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HIBA az indítás során: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                MessageBox.Show(
                    $"Hiba az alkalmazás indításakor:\n\n{ex.Message}",
                    "Kritikus hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                Shutdown();
            }

            base.OnStartup(e);
        }

        private async Task HandleEmailConfirmationCallback(string callbackUrl)
        {
            try
            {
                Debug.WriteLine($"📧 Email confirmation callback feldolgozása: {callbackUrl}");

                // Supabase inicializálás
                var supabaseClient = _host.Services.GetRequiredService<Supabase.Client>();
                await supabaseClient.InitializeAsync();

                var authService = _host.Services.GetRequiredService<IAuthService>();

                // URL parsing: memosphere://auth/callback#access_token=...&refresh_token=...
                var uri = new Uri(callbackUrl);
                var fragment = uri.Fragment.TrimStart('#');

                if (string.IsNullOrEmpty(fragment))
                {
                    Debug.WriteLine("❌ Callback URL, de nincs fragment (hiányzó tokenek)");
                    MessageBox.Show(
                        "Hibás megerősítő link. Kérlek, próbáld újra a regisztrációt.",
                        "Email megerősítés sikertelen",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    Shutdown();
                    return;
                }

                // Tokenek kinyerése
                var parameters = System.Web.HttpUtility.ParseQueryString(fragment);
                var accessToken = parameters["access_token"];
                var refreshToken = parameters["refresh_token"];

                Debug.WriteLine($"Access Token: {(string.IsNullOrEmpty(accessToken) ? "NINCS" : "VAN")}");
                Debug.WriteLine($"Refresh Token: {(string.IsNullOrEmpty(refreshToken) ? "NINCS" : "VAN")}");

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                {
                    Debug.WriteLine("❌ Hiányzó tokenek a callback URL-ből");
                    MessageBox.Show(
                        "Hibás megerősítő link formátum.",
                        "Email megerősítés sikertelen",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    Shutdown();
                    return;
                }

                // Session beállítása a tokenekkel
                var success = await authService.CompleteGoogleSignInAsync(accessToken, refreshToken);

                if (success)
                {
                    Debug.WriteLine("✅ Email megerősítés sikeres, session beállítva");

                    MessageBox.Show(
                        "Email cím sikeresen megerősítve!\n\nMost már be tudsz jelentkezni.",
                        "Email megerősítés sikeres",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    // Adatbázis migráció
                    using (var scope = _host.Services.CreateScope())
                    {
                        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MemoSphereDbContext>>();
                        using var dbContext = factory.CreateDbContext();
                        await dbContext.Database.MigrateAsync();
                    }

                    // MainWindow megnyitása
                    var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                    await mainWindow.LoadDataAsync();
                    mainWindow.Show();
                }
                else
                {
                    Debug.WriteLine("❌ Session beállítása sikertelen");
                    MessageBox.Show(
                        "Hiba történt az email megerősítése során.",
                        "Email megerősítés sikertelen",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Email confirmation callback hiba: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                MessageBox.Show(
                    $"Hiba az email megerősítése során:\n\n{ex.Message}",
                    "Hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                Shutdown();
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            base.OnExit(e);
        }
    }
}