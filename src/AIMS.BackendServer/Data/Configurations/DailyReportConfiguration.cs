using AIMS.BackendServer.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIMS.BackendServer.Data.Configurations;

public class DailyReportConfiguration : IEntityTypeConfiguration<DailyReport>
{
    public void Configure(EntityTypeBuilder<DailyReport> builder)
    {
        // Mỗi intern chỉ có 1 báo cáo mỗi ngày
        builder.HasIndex(x => new { x.InternUserId, x.ReportDate }).IsUnique();

        builder.HasOne(x => x.InternUser)
            .WithMany()
            .HasForeignKey(x => x.InternUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedByMentor)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByMentorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}