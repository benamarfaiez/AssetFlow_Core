using AssetFlowCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetFlowCore.Infrastructure.Configuration;

public class TicketTransferHistoryConfiguration : IEntityTypeConfiguration<TicketTransferHistory>
{
    public void Configure(EntityTypeBuilder<TicketTransferHistory> builder)
    {
        builder.ToTable("t_ticket_transfer_histories");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.MaintenanceTicketId).HasColumnName("maintenance_ticket_id").IsRequired();
        builder.Property(h => h.FromTeamId).HasColumnName("from_team_id").IsRequired();
        builder.Property(h => h.ToTeamId).HasColumnName("to_team_id").IsRequired();
        builder.Property(h => h.Reason).HasColumnName("reason").HasMaxLength(1000).IsRequired();
        builder.Property(h => h.TransferredAt).HasColumnName("transferred_at").HasColumnType("datetime2");

        builder.HasIndex(h => h.MaintenanceTicketId)
            .HasDatabaseName("IX_t_ticket_transfer_histories_maintenance_ticket_id");

        // Pas de FK vers Team : une équipe historiquement transférée mais supprimée depuis ne
        // doit pas bloquer sa suppression, laquelle ne vérifie que les tickets actifs assignés.
        //
        // Relation anonyme côté MaintenanceTicket (pas de .WithMany(t => t.TransferHistory)) :
        // TransferHistory est ignorée par MaintenanceTicketConfiguration et gérée explicitement
        // par le repository (AddTransferHistoryAsync / GetTransferHistoryAsync), jamais par la
        // découverte en cascade d'EF.
        builder.HasOne<MaintenanceTicket>()
           .WithMany()
           .HasForeignKey(h => h.MaintenanceTicketId)
           .OnDelete(DeleteBehavior.Restrict);
    }
}
