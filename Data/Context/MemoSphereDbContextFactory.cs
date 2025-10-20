using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Data.Context
{
    public class MemoSphereDbContextFactory : IDesignTimeDbContextFactory<MemoSphereDbContext>
    {
        public MemoSphereDbContext CreateDbContext(string[] args)
        {
            var basePath = Path.Combine(Directory.GetParent(AppContext.BaseDirectory)!.FullName, "../../../../../MemoSphere");

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                // .AddUserSecrets("4aa70949-6fcd-46fa-8050-04c29ca3a14d") // ← ELTÁVOLÍTVA
                .AddEnvironmentVariables() // Ez marad, environment variable-ekből olvassa
                .Build();

            var connectionString =
                Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING") ??
                configuration.GetConnectionString("Supabase") ??
                configuration["Supabase:ConnectionString"];

            Console.WriteLine("==== CONNECTION STRING DEBUG ====");
            Console.WriteLine($"Connection String: {connectionString}");
            Console.WriteLine($"Contains 'tcp://': {connectionString?.Contains("tcp://")}");
            Console.WriteLine("=================================");

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("PostgreSQL kapcsolati string hiányzik az environment variable-ekből vagy appsettings.json-ből.");

            var optionsBuilder = new DbContextOptionsBuilder<MemoSphereDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new MemoSphereDbContext(optionsBuilder.Options);
        }
    }
}