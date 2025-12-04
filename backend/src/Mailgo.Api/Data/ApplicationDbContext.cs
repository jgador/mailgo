using EmailMarketing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmailMarketing.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<Recipient> Recipients => Set<Recipient>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignSendLog> CampaignSendLogs => Set<CampaignSendLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Recipient>()
            .HasIndex(r => r.Email)
            .IsUnique();

        modelBuilder.Entity<Campaign>()
            .Property(c => c.TargetRecipientCount)
            .HasDefaultValue(0);

        modelBuilder.Entity<CampaignSendLog>()
            .HasIndex(l => new { l.CampaignId, l.RecipientId });
    }
}
