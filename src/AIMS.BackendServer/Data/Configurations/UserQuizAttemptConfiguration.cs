using AIMS.BackendServer.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

// ⚠️ Namespace phải khớp với project của bạn
namespace AIMS.BackendServer.Data.Configurations;

public class UserQuizAttemptConfiguration : IEntityTypeConfiguration<UserQuizAttempt>
{
    public void Configure(EntityTypeBuilder<UserQuizAttempt> builder)
    {
        builder.Property(x => x.TotalScore)
               .HasPrecision(5, 2);  // ← Dòng này quan trọng

        builder.HasOne(x => x.InternUser)
            .WithMany()
            .HasForeignKey(x => x.InternUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}