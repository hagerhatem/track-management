using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrackManagement.Domain.Entities;

namespace TrackManagement.Infrastructure.Persistence.Configurations;

public class TrackDistributionConfiguration : IEntityTypeConfiguration<TrackDistribution>
{
    public void Configure(EntityTypeBuilder<TrackDistribution> builder)
    {
        builder.ToTable("TrackDistributions");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.SubmittedAt).IsRequired();
        builder.Property(d => d.Status).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(d => new { d.TrackId, d.DspId }).IsUnique();

        builder.HasOne(d => d.Track)
            .WithMany(t => t.Distributions)
            .HasForeignKey(d => d.TrackId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Dsp)
            .WithMany(p => p.Distributions)
            .HasForeignKey(d => d.DspId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
