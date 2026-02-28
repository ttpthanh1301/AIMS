using AIMS.BackendServer.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIMS.BackendServer.Data.Configurations;

public class TimesheetConfiguration : IEntityTypeConfiguration<Timesheet>
{
    public void Configure(EntityTypeBuilder<Timesheet> builder)
    {
        builder.Property(x => x.HoursWorked).HasPrecision(4, 2);

        builder.HasOne(x => x.InternUser)
            .WithMany()
            .HasForeignKey(x => x.InternUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Task)
            .WithMany(t => t.Timesheets)
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}