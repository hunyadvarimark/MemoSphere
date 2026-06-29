using Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Data.Services
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddMemoSphereServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddTransient<ISubjectService, SubjectService>();
            services.AddTransient<ITopicService, TopicService>();
            services.AddTransient<INoteService, NoteService>();
            services.AddTransient<IQuestionService, QuestionService>();
            services.AddTransient<IAnswerService, AnswerService>();
            services.AddTransient<IQuizService, QuizService>();
            services.AddTransient<IActiveLearningService, ActiveLearningService>();
            services.AddTransient<INoteShareService, NoteShareService>();
            //services.AddTransient<IAuthService, AuthService>();

            services.AddTransient<IQuestionGeneratorService, GeminiService>(provider =>
            {
                var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                             ?? configuration["GeminiApi:ApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException(
                        "A Gemini API kulcs hiányzik.\n" +
                        "Állítsd be a GEMINI_API_KEY környezeti változót."
                    );
                }

                return new GeminiService(apiKey);
            });

            services.AddTransient<IDocumentImportService>(sp =>
                new DocumentImportService(sp.GetRequiredService<IQuestionGeneratorService>())
            );

            return services;
        }
    }
}