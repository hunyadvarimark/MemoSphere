using Data.Context;
using Data.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;

namespace MemoSphere.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                });
            builder.Services.AddSwaggerGen();

            var connectionString = Environment.GetEnvironmentVariable("LOCAL_DOCKER_CONNECTION_STRING")
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContextFactory<MemoSphereDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddIdentityCore<IdentityUser<Guid>>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 4;
            })
                .AddEntityFrameworkStores<MemoSphereDbContext>();

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                    ValidAudience = builder.Configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"] ?? "NAGYON_TITKOS_MOCK_KULCS_AMIT_KI_KELL_CSERELNI_MAJD"))
                };
            });

            builder.Services.AddMemoSphereServices(builder.Configuration);
            builder.Services.AddScoped<Core.Interfaces.Services.IAuthService, AuthService>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<MemoSphereDbContext>();

                    Console.WriteLine("🔄 Adatbázis migrációk ellenőrzése és futtatása...");
                    context.Database.Migrate();

                    var testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
                    var testUserExists = context.Users.Any(u => u.Id == testUserId);

                    if (!testUserExists)
                    {
                        Console.WriteLine("🤖 Teszt felhasználó nem található. Automatikus létrehozás folyamatban...");

                        var testUser = new Microsoft.AspNetCore.Identity.IdentityUser<Guid>
                        {
                            Id = testUserId,
                            UserName = "fejleszto.teszt@memosphere.local",
                            NormalizedUserName = "FEJLESZTO.TESZT@MEMOSPHERE.LOCAL",
                            Email = "fejleszto.teszt@memosphere.local",
                            NormalizedEmail = "FEJLESZTO.TESZT@MEMOSPHERE.LOCAL",
                            EmailConfirmed = true,
                            SecurityStamp = Guid.NewGuid().ToString()
                        };

                        var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Microsoft.AspNetCore.Identity.IdentityUser<Guid>>();
                        testUser.PasswordHash = passwordHasher.HashPassword(testUser, "Admin123");

                        context.Users.Add(testUser);
                        context.SaveChanges();

                        Console.WriteLine("✅ Teszt felhasználó sikeresen legenerálva a Docker Postgresben!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Hiba történt a Data Seeding során: {ex.Message}");
                }
            }

            app.Run();
        }
    }
}
