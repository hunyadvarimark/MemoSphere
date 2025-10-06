using Core.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class MemoSphereDbContextFactory : IDesignTimeDbContextFactory<MemoSphereDbContext>
{
    public MemoSphereDbContext CreateDbContext(string[] args)
    {
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "MemoSphere.db");
        var optionsBuilder = new DbContextOptionsBuilder<MemoSphereDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        return new MemoSphereDbContext(optionsBuilder.Options);
    }
}
