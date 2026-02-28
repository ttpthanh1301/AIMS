using AIMS.BackendServer.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIMS.BackendServer.Data.Configurations;

public class InternAssignmentConfiguration : IEntityTypeConfiguration<InternAssignment>
{
    public void Configure(EntityTypeBuilder<InternAssignment> builder)
    {
        // Mỗi intern chỉ có 1 mentor trong 1 kỳ
        builder.HasIndex(x => new { x.InternUserId, x.PeriodId }).IsUnique();

        // Tránh cascade delete vòng
        builder.HasOne(x => x.InternUser)
            .WithMany()
            .HasForeignKey(x => x.InternUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MentorUser)
            .WithMany()
            .HasForeignKey(x => x.MentorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}