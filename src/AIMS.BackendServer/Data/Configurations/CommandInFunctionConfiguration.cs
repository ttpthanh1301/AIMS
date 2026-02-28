using AIMS.BackendServer.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIMS.BackendServer.Data.Configurations;

public class CommandInFunctionConfiguration : IEntityTypeConfiguration<CommandInFunction>
{
    public void Configure(EntityTypeBuilder<CommandInFunction> builder)
    {
        // Khóa chính tổ hợp
        builder.HasKey(x => new { x.CommandId, x.FunctionId });

        builder.HasOne(x => x.Command)
            .WithMany(c => c.CommandInFunctions)
            .HasForeignKey(x => x.CommandId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Function)
            .WithMany()
            .HasForeignKey(x => x.FunctionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}