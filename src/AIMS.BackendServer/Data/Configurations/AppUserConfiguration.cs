using AIMS.BackendServer.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIMS.BackendServer.Data.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.StudentId).HasMaxLength(50);
        builder.Property(x => x.GPA).HasColumnType("decimal(3,2)");
        builder.Property(x => x.CVFileUrl).HasMaxLength(500);

        builder.HasOne(x => x.University)
            .WithMany(u => u.Users)
            .HasForeignKey(x => x.UniversityId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}