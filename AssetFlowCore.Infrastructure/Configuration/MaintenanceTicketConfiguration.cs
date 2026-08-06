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

        builder.Property(t => t.AssignedTeamId)
            .HasColumnName("assigned_team_id")
            .IsRequired();

        builder.Property(t => t.AssetId)
            .HasColumnName("asset_id")
            .IsRequired();

        builder.Property(t => t.ResolutionComment).HasColumnName("resolution_comment").HasColumnType("nvarchar(max)");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2");

        // Indicateur d'état pour le Worker asynchrone
        builder.Property(t => t.IsAiProcessing)
            .HasColumnName("is_ai_processing")
            .HasDefaultValue(false) // Par défaut à false à la création
            .IsRequired();

        // Le rapport Markdown peut être très volumineux, on mappe vers le type texte maximal
        builder.Property(t => t.AssistanceNote)
            .HasColumnName("assistance_note")
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        // Gestion de la concurrence optimiste via jeton d'infrastructure
        builder.Property(t => t.RowVersion).HasColumnName("row_version").IsRowVersion();

        // Lot 7 (décision 0.2) : traçabilité de l'auteur, additive et nullable (tickets historiques sans auteur)
        builder.Property(t => t.AssignedByUserId).HasColumnName("assigned_by_user_id");
        builder.Property(t => t.ClosedByUserId).HasColumnName("closed_by_user_id");

        builder.HasIndex(t => new { t.AssetId, t.Status })
            .HasDatabaseName("IX_t_maintenance_tickets_asset_id_status");

        builder.HasIndex(t => t.AssignedTeamId)
            .HasDatabaseName("IX_t_tickets_assigned_team_id");

        // ── Relations (Clés Étrangères) ───────────────────────────────

        // 1. MaintenanceTicket -> Team
        builder.HasOne(t => t.AssignedTeam)
           .WithMany(team => team.Tickets)
           .HasForeignKey(t => t.AssignedTeamId)
           .OnDelete(DeleteBehavior.Restrict);

        // 2. MaintenanceTicket -> Asset
        builder.HasOne(t => t.Asset)
           .WithMany(asset => asset.Tickets)
           .HasForeignKey(t => t.AssetId)
           .OnDelete(DeleteBehavior.Restrict);

        // 3. MaintenanceTicket -> User (auteurs de la prise en charge / de la clôture, Lot 7)
        builder.HasOne<User>()
           .WithMany()
           .HasForeignKey(t => t.AssignedByUserId)
           .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
           .WithMany()
           .HasForeignKey(t => t.ClosedByUserId)
           .OnDelete(DeleteBehavior.Restrict);
    }
}