using AssetFlowCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetFlowCore.Infrastructure.Configuration;

public class MaintenanceTicketConfiguration : IEntityTypeConfiguration<MaintenanceTicket>
{
    public void Configure(EntityTypeBuilder<MaintenanceTicket> builder)
    {
        builder.ToTable("t_maintenance_tickets");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.Title).HasColumnName("title").HasMaxLength(150).IsRequired();
        builder.Property(t => t.Description).HasColumnName("description").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(t => t.Criticality).HasColumnName("criticality").HasMaxLength(20).HasConversion<string>();
        builder.Property(t => t.Status).HasColumnName("status").HasMaxLength(30).HasConversion<string>();
        builder.Property(t => t.AssignedTeam).HasColumnName("assigned_team").HasMaxLength(50).IsRequired();
        builder.Property(t => t.ResolutionComment).HasColumnName("resolution_comment").HasColumnType("nvarchar(max)");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2");

        // Gestion de la concurrence optimiste via jeton d'infrastructure
        builder.Property(t => t.RowVersion).HasColumnName("row_version").IsRowVersion();

        builder.HasIndex(t => new { t.AssetId, t.Status })
            .HasDatabaseName("IX_t_maintenance_tickets_asset_id_status"); ;
    }
}