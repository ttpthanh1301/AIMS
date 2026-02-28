using AIMS.BackendServer.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIMS.BackendServer.Data.Configurations;

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.HasIndex(x => new { x.InternUserId, x.CourseId }).IsUnique();
        builder.Property(x => x.CertificateCode).HasMaxLength(100);

        builder.HasOne(x => x.InternUser)
            .WithMany()
            .HasForeignKey(x => x.InternUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Attempt)
            .WithOne(a => a.Certificate)
            .HasForeignKey<Certificate>(x => x.AttemptId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}