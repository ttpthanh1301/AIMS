using AIMS.BackendServer.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIMS.BackendServer.Data.Configurations;

public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.Property(x => x.CVFileUrl).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("PENDING");

        // ← Fix cascade
        builder.HasOne(x => x.ApplicantUser)
            .WithMany()
            .HasForeignKey(x => x.ApplicantUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.JobDescription)
            .WithMany(j => j.Applications)
            .HasForeignKey(x => x.JobDescriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Quan hệ 1-1 với CVParsedData
        builder.HasOne(x => x.CVParsedData)
            .WithOne(c => c.Application)
            .HasForeignKey<CVParsedData>(c => c.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Quan hệ 1-1 với AIScreeningResult
        builder.HasOne(x => x.AIScreeningResult)
            .WithOne(a => a.Application)
            .HasForeignKey<AIScreeningResult>(a => a.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}