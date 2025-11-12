using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace EduvisionMvc.Data
{
    // Design-time factory ensures migrations are generated against SQL Server (Production provider)
    // IMPORTANT: Do NOT put real production secrets here. Use a local dev SQL Server instance or override via CLI:
    //   dotnet ef migrations add SqlServerInitial -c AppDbContext 
    // If you want to use a different connection string: 
    //   setx EDUVISION_MIGRATIONS_CS "Server=(localdb)\\mssqllocaldb;Database=EduvisionDesign;Trusted_Connection=True;TrustServerCertificate=True" 
    // Then reopen your shell before running the command.
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Prefer env variable override, else fallback.
            var cs = Environment.GetEnvironmentVariable("EDUVISION_MIGRATIONS_CS")
                     ?? "Server=(localdb)\\mssqllocaldb;Database=EduvisionDesign;Trusted_Connection=True;TrustServerCertificate=True";

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(cs);
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
