using AssetFlowCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetFlowCore.Infrastructure.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("t_users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(u => u.ExternalId)
            .HasColumnName("external_id")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(320);

        builder.Property(u => u.TeamId)
            .HasColumnName("team_id");

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2");

        builder.HasIndex(u => u.ExternalId)
            .IsUnique()
            .HasDatabaseName("IX_t_users_external_id");

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(u => u.TeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
