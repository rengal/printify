using Microsoft.EntityFrameworkCore;
using Printify.Infrastructure.Persistence.Entities.AnonymousSessions;
using Printify.Infrastructure.Persistence.Entities.PrinterJobs;
using Printify.Infrastructure.Persistence.Entities.Printers;
using Printify.Infrastructure.Persistence.Entities.Users;

namespace Printify.Infrastructure.Persistence;

public sealed class PrintifyDbContext : DbContext
{
    public PrintifyDbContext(DbContextOptions<PrintifyDbContext> options)
        : base(options)
    {
    }

    public DbSet<AnonymousSessionEntity> AnonymousSessions => Set<AnonymousSessionEntity>();

    public DbSet<UserEntity> Users => Set<UserEntity>();

    public DbSet<PrinterEntity> Printers => Set<PrinterEntity>();

    public DbSet<PrintJobEntity> PrintJobs => Set<PrintJobEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AnonymousSessionEntity>()
            .HasOne(session => session.LinkedUser)
            .WithMany()
            .HasForeignKey(session => session.LinkedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PrinterEntity>()
            .HasIndex(printer => printer.DisplayName);
    }
}
