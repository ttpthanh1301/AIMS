using AIMS.BackendServer.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIMS.BackendServer.Data.Configurations;

public class UserQuizAnswerConfiguration : IEntityTypeConfiguration<UserQuizAnswer>
{
    public void Configure(EntityTypeBuilder<UserQuizAnswer> builder)
    {
        builder.HasOne(x => x.Attempt)
            .WithMany(a => a.Answers)
            .HasForeignKey(x => x.AttemptId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Question)
            .WithMany()
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SelectedOption)
            .WithMany()
            .HasForeignKey(x => x.SelectedOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Decimal precision for mentor-assigned scores to avoid store truncation warnings
        builder.Property(x => x.MentorScore)
            .HasPrecision(18, 2);
    }
}