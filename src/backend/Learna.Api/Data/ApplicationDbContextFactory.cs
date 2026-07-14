using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Learna.Infrastructure.Data;

namespace Learna.Api.Data;

// Used by `dotnet ef` design-time tooling so migrations can be generated
// without a live database connection (ServerVersion.AutoDetect requires one).
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseMySql(
            "Server=localhost;Database=learna_dev;User=root;Password=root;",
            new MySqlServerVersion(new Version(8, 0, 21)));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
