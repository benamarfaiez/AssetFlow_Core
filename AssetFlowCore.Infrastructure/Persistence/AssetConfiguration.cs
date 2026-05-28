using AssetFlowCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetFlowCore.Infrastructure.Persistence;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("t_assets");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.Name).HasColumnName("name").HasMaxLength(100).IsRequired();

        builder.OwnsOne(a => a.SerialNumber, sb =>
        {
            sb.Property(s => s.Value).HasColumnName("serial_num").HasMaxLength(50).IsRequired();
            sb.HasIndex(s => s.Value).IsUnique();
        });

        builder.Property(a => a.Type).HasColumnName("type").HasMaxLength(50).HasConversion<string>();
        builder.Property(a => a.Status).HasColumnName("status").HasMaxLength(30).HasConversion<string>();
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2");

        builder.HasMany(a => a.Tickets).WithOne().HasForeignKey(t => t.AssetId).OnDelete(DeleteBehavior.Restrict);
    }
}