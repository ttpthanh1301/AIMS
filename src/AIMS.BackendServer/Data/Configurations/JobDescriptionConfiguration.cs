using AIMS.BackendServer.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIMS.BackendServer.Data.Configurations;

public class JobDescriptionConfiguration : IEntityTypeConfiguration<JobDescription>
{
    public void Configure(EntityTypeBuilder<JobDescription> builder)
    {
        builder.Property(x => x.MinGPA).HasPrecision(3, 2);
        builder.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("OPEN");

        builder.HasOne(x => x.JobPosition)
            .WithMany(j => j.JobDescriptions)
            .HasForeignKey(x => x.JobPositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}