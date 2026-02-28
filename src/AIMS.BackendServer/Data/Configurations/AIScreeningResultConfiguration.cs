using AIMS.BackendServer.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIMS.BackendServer.Data.Configurations;

public class AIScreeningResultConfiguration : IEntityTypeConfiguration<AIScreeningResult>
{
    public void Configure(EntityTypeBuilder<AIScreeningResult> builder)
    {
        builder.Property(x => x.MatchingScore).HasPrecision(5, 2);

        builder.HasOne(x => x.ReviewedByHR)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByHRId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}