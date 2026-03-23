using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AIMS.BackendServer.UnitTests.Integration;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // ── Thay SQL Server bằng InMemory DB ─────────────
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AimsDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AimsDbContext>(opts =>
                opts.UseInMemoryDatabase("IntegrationTestDb_" +
                    Guid.NewGuid().ToString()));

            // ── Seed test data ────────────────────────────────
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AimsDbContext>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

            context.Database.EnsureCreated();
            SeedTestDataAsync(context, userMgr, roleMgr).Wait();
        });

        builder.UseEnvironment("Testing");
    }

    // ── Seed data tối thiểu cho test ──────────────────────────
    private static async Task SeedTestDataAsync(
        AimsDbContext context,
        UserManager<AppUser> userMgr,
        RoleManager<AppRole> roleMgr)
    {
        // Roles
        foreach (var (id, name) in new[] {
            ("admin", "Admin"), ("hr", "HR"),
            ("mentor", "Mentor"), ("intern", "Intern") })
        {
            if (!await roleMgr.RoleExistsAsync(name))
                await roleMgr.CreateAsync(new AppRole
                {
                    Id = id,
                    Name = name,
                    Description = name
                });
        }

        // Admin user
        if (await userMgr.FindByEmailAsync("admin@test.vn") == null)
        {
            var admin = new AppUser
            {
                Id = "admin-test-001",
                UserName = "admin@test.vn",
                Email = "admin@test.vn",
                FirstName = "Admin",
                LastName = "Test",
                IsActive = true,
                EmailConfirmed = true,
            };
            await userMgr.CreateAsync(admin, "Admin@2025!");
            await userMgr.AddToRoleAsync(admin, "Admin");
        }

        // HR user
        if (await userMgr.FindByEmailAsync("hr@test.vn") == null)
        {
            var hr = new AppUser
            {
                Id = "hr-test-001",
                UserName = "hr@test.vn",
                Email = "hr@test.vn",
                FirstName = "HR",
                LastName = "Test",
                IsActive = true,
                EmailConfirmed = true,
            };
            await userMgr.CreateAsync(hr, "Hr@2025!");
            await userMgr.AddToRoleAsync(hr, "HR");
        }

        // Functions
        if (!context.Functions.Any())
        {
            context.Functions.AddRange(
                new Data.Entities.Function
                {
                    Id = "SYSTEM_ROLE",
                    Name = "Quản lý Role",
                    Url = "/system/roles",
                    SortOrder = 1
                },
                new Data.Entities.Function
                {
                    Id = "SYSTEM_USER",
                    Name = "Quản lý User",
                    Url = "/system/users",
                    SortOrder = 2
                },
                new Data.Entities.Function
                {
                    Id = "RECRUITMENT_JD",
                    Name = "Job Descriptions",
                    Url = "/recruitment/jd",
                    SortOrder = 3
                }
            );
        }

        // Commands
        if (!context.Commands.Any())
        {
            context.Commands.AddRange(
                new Data.Entities.Command { Id = "VIEW", Name = "Xem" },
                new Data.Entities.Command { Id = "CREATE", Name = "Tạo" },
                new Data.Entities.Command { Id = "DELETE", Name = "Xóa" }
            );
        }

        await context.SaveChangesAsync();

        // Permissions cho Admin (tất cả)
        if (!context.Permissions.Any())
        {
            var adminRole = await roleMgr.FindByNameAsync("Admin");
            context.Permissions.AddRange(
                new Data.Entities.Permission
                {
                    RoleId = adminRole!.Id,
                    FunctionId = "SYSTEM_ROLE",
                    CommandId = "VIEW"
                },
                new Data.Entities.Permission
                {
                    RoleId = adminRole.Id,
                    FunctionId = "SYSTEM_ROLE",
                    CommandId = "DELETE"
                },
                new Data.Entities.Permission
                {
                    RoleId = adminRole.Id,
                    FunctionId = "SYSTEM_USER",
                    CommandId = "VIEW"
                }
            );

            // HR chỉ có VIEW RECRUITMENT_JD
            var hrRole = await roleMgr.FindByNameAsync("HR");
            context.Permissions.Add(
                new Data.Entities.Permission
                {
                    RoleId = hrRole!.Id,
                    FunctionId = "RECRUITMENT_JD",
                    CommandId = "VIEW"
                }
            );

            await context.SaveChangesAsync();
        }
    }
}