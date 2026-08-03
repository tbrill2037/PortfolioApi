using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Entities.Email;
using Portfolio.Infra.Identity;

namespace Portfolio.Infra.Data;

/// <summary>
/// Represents the application's Entity Framework database context.
/// </summary>
public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<EmailMessageEntity> EmailMessages { get; set; }
    public DbSet<EmailRequestEntity> EmailRequests { get; set; }
    public DbSet<EmailEventEntity> EmailEvents { get; set; }
    public DbSet<EmailOutboxEntity> EmailOutboxes { get; set; }
    public DbSet<EmailDeadLetterEntity> EmailDeadLetters { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> dbContextOptions)
        : base(dbContextOptions)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EmailMessageEntity>(entity =>
        {
            entity.HasMany(e => e.Events)
                .WithOne(e => e.EmailMessage)
                .HasForeignKey(e => e.EmailMessageId)
                .OnDelete(DeleteBehavior.Cascade);

            // SES message IDs identify accepted email requests and are unique.
            entity.HasIndex(x => x.SesMessageId)
                .IsUnique()
                .HasFilter("[SesMessageId] IS NOT NULL");

            // Supports processing lock recovery and worker queries.
            entity.HasIndex(x => new
            {
                x.Status,
                x.ProcessingLockUntilUtc,
            });

            // Supports retry processing.
            entity.HasIndex(x => new
            {
                x.Status,
                x.LastAttemptUtc,
            });

            entity.HasIndex(x => x.CorrelationId);
        });

        modelBuilder.Entity<EmailEventEntity>(entity =>
        {
            // SES may deliver duplicate events. Prevent duplicate event records.
            entity.HasIndex(x => new
            {
                x.SesMessageId,
                x.EventType,
            })
            .IsUnique();

            entity.HasIndex(x => x.EmailMessageId);
        });

        modelBuilder.Entity<EmailOutboxEntity>(entity =>
        {
            // Supports pending outbox processing and lock recovery.
            entity.HasIndex(x => new
            {
                x.ProcessedUtc,
                x.ProcessingLockUntilUtc,
                x.CreatedUtc
            });

            entity.HasIndex(x => x.EmailMessageId)
                .IsUnique();

            entity.HasIndex(x => x.ProcessingLockToken);
        });

        modelBuilder.Entity<EmailDeadLetterEntity>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.EmailMessageId);

            entity.HasIndex(x => x.SqsMessageId)
                .IsUnique();
        });
    }
}