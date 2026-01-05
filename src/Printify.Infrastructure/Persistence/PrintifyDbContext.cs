using Microsoft.EntityFrameworkCore;
using Printify.Infrastructure.Persistence.Entities.Documents;
using Printify.Infrastructure.Persistence.Entities.PrinterJobs;
using Printify.Infrastructure.Persistence.Entities.Printers;
using Printify.Infrastructure.Persistence.Entities.Workspaces;

namespace Printify.Infrastructure.Persistence;

public sealed class PrintifyDbContext : DbContext
{
    public PrintifyDbContext(DbContextOptions<PrintifyDbContext> options)
        : base(options)
    {
    }

    public DbSet<WorkspaceEntity> Workspaces => Set<WorkspaceEntity>();

    public DbSet<PrinterEntity> Printers => Set<PrinterEntity>();

    public DbSet<PrinterOperationalFlagsEntity> PrinterOperationalFlags => Set<PrinterOperationalFlagsEntity>();

    public DbSet<PrintJobEntity> PrintJobs => Set<PrintJobEntity>();

    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();

    public DbSet<DocumentMediaEntity> DocumentMedia => Set<DocumentMediaEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PrinterEntity>()
            .HasIndex(printer => printer.DisplayName);

        modelBuilder.Entity<PrinterEntity>()
            .OwnsOne(
                printer => printer.Settings,
                settings =>
                {
                    settings.HasIndex(setting => setting.ListenTcpPortNumber)
                        .IsUnique();
                });

        modelBuilder.Entity<PrinterOperationalFlagsEntity>(entity =>
        {
            entity.HasKey(status => status.PrinterId);

            entity.HasOne<PrinterEntity>()
                .WithOne()
                .HasForeignKey<PrinterOperationalFlagsEntity>(status => status.PrinterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentEntity>(entity =>
        {
            entity.Property(document => document.Protocol)
                .IsRequired();

            entity.HasMany(document => document.Elements)
                .WithOne(element => element.Document)
                .HasForeignKey(element => element.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(document => new { document.PrinterId, document.CreatedAtUnixMs, document.Id })
                .HasDatabaseName("IX_documents_printer_created_at_id");
        });

        modelBuilder.Entity<DocumentElementEntity>(entity =>
        {
            entity.Property(element => element.Payload)
                .IsRequired();

            entity.Property(element => element.ElementType)
                .IsRequired();

            entity.HasIndex(element => new { element.DocumentId, element.Sequence })
                .IsUnique();

            entity.HasOne(element => element.Media)
                .WithMany()
                .HasForeignKey(element => element.MediaId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DocumentMediaEntity>(entity =>
        {
            entity.Property(media => media.ContentType)
                .IsRequired();

            entity.Property(media => media.Url)
                .IsRequired();

            entity.HasIndex(media => media.Checksum);
        });
    }
}
