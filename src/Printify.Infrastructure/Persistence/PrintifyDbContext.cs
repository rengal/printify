using Microsoft.EntityFrameworkCore;
using Printify.Infrastructure.Persistence.Entities.AnonymousSessions;
using Printify.Infrastructure.Persistence.Entities.Documents;
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

    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();

    public DbSet<DocumentElementEntity> DocumentElements => Set<DocumentElementEntity>();

    public DbSet<DocumentMediaEntity> DocumentMedia => Set<DocumentMediaEntity>();

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

        modelBuilder.Entity<DocumentEntity>(entity =>
        {
            entity.Property(document => document.Protocol)
                .IsRequired();

            entity.HasMany(document => document.Elements)
                .WithOne(element => element.Document)
                .HasForeignKey(element => element.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(document => new { document.PrinterId, document.CreatedAt, document.Id })
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
                .WithOne(media => media.Element)
                .HasForeignKey<DocumentMediaEntity>(media => media.DocumentElementId)
                .OnDelete(DeleteBehavior.Cascade);
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
