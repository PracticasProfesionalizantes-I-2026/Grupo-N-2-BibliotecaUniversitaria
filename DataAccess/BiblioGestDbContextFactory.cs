using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BiblioGest.DataAccess;

public class BiblioGestDbContextFactory : IDesignTimeDbContextFactory<BiblioGestDbContext>
{
    public BiblioGestDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BiblioGestDbContext>();
        optionsBuilder.UseSqlite("Data Source=bibliogest.db");
        return new BiblioGestDbContext(optionsBuilder.Options);
    }
}
