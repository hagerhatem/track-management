using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrackManagement.Domain.Entities;

namespace TrackManagement.Infrastructure.Persistence.Configurations;

public class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.ToTable("Tracks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(300);
        builder.Property(t => t.Isrc).IsRequired().HasMaxLength(12).IsUnicode(false);
        builder.Property(t => t.ReleaseDate).IsRequired();
        builder.Property(t => t.Genre).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Status).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(t => t.Isrc).IsUnique();

        builder.HasOne(t => t.Artist)
            .WithMany(a => a.Tracks)
            .HasForeignKey(t => t.ArtistId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
