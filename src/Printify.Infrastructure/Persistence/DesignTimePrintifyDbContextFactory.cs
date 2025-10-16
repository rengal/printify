using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Printify.Infrastructure.Persistence;

internal sealed class DesignTimePrintifyDbContextFactory : IDesignTimeDbContextFactory<PrintifyDbContext>
{
    public PrintifyDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PrintifyDbContext>();
        optionsBuilder.UseSqlite("Data Source=printify.db");
        return new PrintifyDbContext(optionsBuilder.Options);
    }
}
