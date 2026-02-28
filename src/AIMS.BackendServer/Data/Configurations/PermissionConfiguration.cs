using AIMS.BackendServer.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIMS.BackendServer.Data.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        // Khóa chính tổ hợp 3 chiều
        builder.HasKey(x => new { x.FunctionId, x.RoleId, x.CommandId });

        builder.HasOne(x => x.Function)
            .WithMany(f => f.Permissions)
            .HasForeignKey(x => x.FunctionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}