// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Mailgo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mailgo.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Recipient> Recipients => Set<Recipient>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignSendLog> CampaignSendLogs => Set<CampaignSendLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Recipient>()
            .ToTable("Recipient")
            .HasIndex(r => r.Email)
            .IsUnique();

        modelBuilder.Entity<Campaign>()
            .ToTable("Campaign")
            .Property(c => c.TargetRecipientCount)
            .HasDefaultValue(0);

        modelBuilder.Entity<CampaignSendLog>()
            .ToTable("CampaignSendLog")
            .HasIndex(l => new { l.CampaignId, l.RecipientId });
    }
}

