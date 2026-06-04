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
                .AddEnvironmentVariables()
                .Build();


            var connectionString = Environment.GetEnvironmentVariable("LOCAL_DOCKER_CONNECTION_STRING");


            Console.WriteLine("==== CONNECTION STRING DEBUG ====");
            Console.WriteLine($"Connection String: {connectionString}");
            Console.WriteLine($"Contains 'tcp://': {connectionString?.Contains("tcp://")}");
            Console.WriteLine("=================================");

            var optionsBuilder = new DbContextOptionsBuilder<MemoSphereDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new MemoSphereDbContext(optionsBuilder.Options);
        }
    }
}