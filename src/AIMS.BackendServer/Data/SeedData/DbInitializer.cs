using AIMS.BackendServer.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AIMS.BackendServer.Data.SeedData;

public static class DbInitializer
{
    public static async Task SeedAsync(
        AimsDbContext context,
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager)
    {
        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager);
        await SeedHrUserAsync(userManager);

        await SeedFunctionsAsync(context);
        await context.SaveChangesAsync(); // ← Save Functions trước

        await SeedCommandsAsync(context);
        await context.SaveChangesAsync(); // ← Save Commands trước

        await SeedCommandInFunctionsAsync(context);
        await context.SaveChangesAsync(); // ← PHẢI save CIF trước khi seed Permissions

        await SeedPermissionsAsync(context, roleManager);
        await context.SaveChangesAsync(); // ← Save Permissions
    }

    // ─────────────────────────────────────────────────────────────
    // 1. ROLES
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedRolesAsync(RoleManager<AppRole> roleManager)
    {
        var roles = new List<AppRole>
        {
            new AppRole { Id = "admin",  Name = "Admin",  NormalizedName = "ADMIN",
                          Description = "Quản trị viên hệ thống, toàn quyền" },
            new AppRole { Id = "hr",     Name = "HR",     NormalizedName = "HR",
                          Description = "Nhân viên tuyển dụng, quản lý ứng viên" },
            new AppRole { Id = "mentor", Name = "Mentor", NormalizedName = "MENTOR",
                          Description = "Người hướng dẫn thực tập sinh" },
            new AppRole { Id = "intern", Name = "Intern", NormalizedName = "INTERN",
                          Description = "Thực tập sinh" },
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name!))
                await roleManager.CreateAsync(role);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 2. ADMIN USER
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedAdminUserAsync(UserManager<AppUser> userManager)
    {
        const string adminEmail = "admin@deha.vn";

        if (await userManager.FindByEmailAsync(adminEmail) != null) return;

        var adminUser = new AppUser
        {
            Id = "admin-seed-001",
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "System",
            LastName = "Admin",
            IsActive = true,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(adminUser, "Admin@2025!");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(adminUser, "Admin");
    }
    // ─────────────────────────────────────────────────────────────
    // HR
    private static async Task SeedHrUserAsync(UserManager<AppUser> userManager)
    {
        const string hrEmail = "hr@deha.vn";

        if (await userManager.FindByEmailAsync(hrEmail) != null) return;

        var adminUser = new AppUser
        {
            Id = "hr-seed-001",
            UserName = hrEmail,
            Email = hrEmail,
            FirstName = "HR",
            LastName = "User",
            IsActive = true,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(adminUser, "Admin@2025!");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(adminUser, "HR");
    }
    // 3. FUNCTIONS (cây menu hệ thống)
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedFunctionsAsync(AimsDbContext context)
    {
        if (await context.Functions.AnyAsync()) return;

        var functions = new List<Function>
        {
            // ── ROOT FUNCTIONS ──────────────────────────────
            new Function { Id = "DASHBOARD",   Name = "Dashboard",    Url = "/dashboard",
                           Icon = "bi bi-speedometer2", SortOrder = 1, ParentId = null },

            new Function { Id = "RECRUITMENT", Name = "Tuyển dụng",   Url = "/recruitment",
                           Icon = "bi bi-person-plus",  SortOrder = 2, ParentId = null },

            new Function { Id = "LMS",         Name = "Đào tạo",      Url = "/lms",
                           Icon = "bi bi-book",         SortOrder = 3, ParentId = null },

            new Function { Id = "TASKS",       Name = "Quản lý Task", Url = "/tasks",
                           Icon = "bi bi-kanban",       SortOrder = 4, ParentId = null },

            new Function { Id = "REPORTS",     Name = "Báo cáo",      Url = "/reports",
                           Icon = "bi bi-bar-chart",    SortOrder = 5, ParentId = null },

            new Function { Id = "SYSTEM",      Name = "Hệ thống",     Url = "/system",
                           Icon = "bi bi-gear",         SortOrder = 6, ParentId = null },

            // ── CHILDREN — RECRUITMENT ──────────────────────
            new Function { Id = "RECRUITMENT_JD",
                           Name = "Job Descriptions", Url = "/recruitment/jd",
                           Icon = "bi bi-file-text",  SortOrder = 1, ParentId = "RECRUITMENT" },

            new Function { Id = "RECRUITMENT_CV",
                           Name = "CV Screening (AI)", Url = "/recruitment/screening",
                           Icon = "bi bi-robot",       SortOrder = 2, ParentId = "RECRUITMENT" },

            new Function { Id = "RECRUITMENT_RANKING",
                           Name = "Ranking ứng viên", Url = "/recruitment/ranking",
                           Icon = "bi bi-trophy",      SortOrder = 3, ParentId = "RECRUITMENT" },

            // ── CHILDREN — LMS ──────────────────────────────
            new Function { Id = "LMS_COURSES",
                           Name = "Khóa học",  Url = "/lms/courses",
                           Icon = "bi bi-collection", SortOrder = 1, ParentId = "LMS" },

            new Function { Id = "LMS_QUIZ",
                           Name = "Bài kiểm tra", Url = "/lms/quiz",
                           Icon = "bi bi-pencil-square", SortOrder = 2, ParentId = "LMS" },

            new Function { Id = "LMS_CERTIFICATE",
                           Name = "Chứng chỉ",  Url = "/lms/certificates",
                           Icon = "bi bi-award", SortOrder = 3, ParentId = "LMS" },

            // ── CHILDREN — TASKS ────────────────────────────
            new Function { Id = "TASKS_BOARD",
                           Name = "Kanban Board", Url = "/tasks/board",
                           Icon = "bi bi-columns-gap", SortOrder = 1, ParentId = "TASKS" },

            new Function { Id = "TASKS_REPORT",
                           Name = "Daily Report", Url = "/tasks/daily-report",
                           Icon = "bi bi-journal-text", SortOrder = 2, ParentId = "TASKS" },

            new Function { Id = "TASKS_TIMESHEET",
                           Name = "Timesheet",    Url = "/tasks/timesheet",
                           Icon = "bi bi-clock",  SortOrder = 3, ParentId = "TASKS" },

            // ── CHILDREN — SYSTEM ───────────────────────────
            new Function { Id = "SYSTEM_USER",
                           Name = "Quản lý User", Url = "/system/users",
                           Icon = "bi bi-people",  SortOrder = 1, ParentId = "SYSTEM" },

            new Function { Id = "SYSTEM_ROLE",
                           Name = "Quản lý Role", Url = "/system/roles",
                           Icon = "bi bi-shield",  SortOrder = 2, ParentId = "SYSTEM" },

            new Function { Id = "SYSTEM_PERMISSION",
                           Name = "Phân quyền",   Url = "/system/permissions",
                           Icon = "bi bi-key",     SortOrder = 3, ParentId = "SYSTEM" },
        };

        await context.Functions.AddRangeAsync(functions);
    }

    // ─────────────────────────────────────────────────────────────
    // 4. COMMANDS (hành động trên từng Function)
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedCommandsAsync(AimsDbContext context)
    {
        if (await context.Commands.AnyAsync()) return;

        var commands = new List<Command>
        {
            new Command { Id = "VIEW",   Name = "Xem" },
            new Command { Id = "CREATE", Name = "Tạo mới" },
            new Command { Id = "UPDATE", Name = "Cập nhật" },
            new Command { Id = "DELETE", Name = "Xóa" },
            new Command { Id = "EXPORT", Name = "Xuất dữ liệu" },
            new Command { Id = "IMPORT", Name = "Nhập dữ liệu" },
            new Command { Id = "APPROVE",Name = "Duyệt / Phê duyệt" },
        };

        await context.Commands.AddRangeAsync(commands);
    }

    // ─────────────────────────────────────────────────────────────
    // 5. COMMAND IN FUNCTIONS (liên kết command ↔ function)
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedCommandInFunctionsAsync(AimsDbContext context)
    {
        if (await context.CommandInFunctions.AnyAsync()) return;

        // Quy tắc: VIEW áp dụng cho tất cả Function
        // CREATE/UPDATE/DELETE áp dụng cho các Function chính
        var allFuncIds = new[]
        {
            "DASHBOARD", "RECRUITMENT", "LMS", "TASKS", "REPORTS", "SYSTEM",
            "RECRUITMENT_JD", "RECRUITMENT_CV", "RECRUITMENT_RANKING",
            "LMS_COURSES", "LMS_QUIZ", "LMS_CERTIFICATE",
            "TASKS_BOARD", "TASKS_REPORT", "TASKS_TIMESHEET",
            "SYSTEM_USER", "SYSTEM_ROLE", "SYSTEM_PERMISSION"
        };

        var crudFuncIds = new[]
        {
            "RECRUITMENT_JD", "LMS_COURSES", "LMS_QUIZ",
            "TASKS_BOARD", "TASKS_REPORT",
            "SYSTEM_USER", "SYSTEM_ROLE", "SYSTEM_PERMISSION"
        };

        var cifs = new List<CommandInFunction>();

        // VIEW cho tất cả
        foreach (var fId in allFuncIds)
            cifs.Add(new CommandInFunction { CommandId = "VIEW", FunctionId = fId });

        // CREATE, UPDATE, DELETE cho các function CRUD
        foreach (var fId in crudFuncIds)
        {
            cifs.Add(new CommandInFunction { CommandId = "CREATE", FunctionId = fId });
            cifs.Add(new CommandInFunction { CommandId = "UPDATE", FunctionId = fId });
            cifs.Add(new CommandInFunction { CommandId = "DELETE", FunctionId = fId });
        }

        // EXPORT cho báo cáo
        cifs.Add(new CommandInFunction { CommandId = "EXPORT", FunctionId = "REPORTS" });
        cifs.Add(new CommandInFunction { CommandId = "EXPORT", FunctionId = "RECRUITMENT_RANKING" });

        // APPROVE cho CV screening
        cifs.Add(new CommandInFunction { CommandId = "APPROVE", FunctionId = "RECRUITMENT_CV" });
        cifs.Add(new CommandInFunction { CommandId = "APPROVE", FunctionId = "TASKS_REPORT" });

        await context.CommandInFunctions.AddRangeAsync(cifs);
    }

    // ─────────────────────────────────────────────────────────────
    // 6. PERMISSIONS (Role Admin có tất cả quyền)
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedPermissionsAsync(
        AimsDbContext context,
        RoleManager<AppRole> roleManager)
    {
        if (await context.Permissions.AnyAsync()) return;

        // ← Dùng FindByNameAsync để lấy đúng Id thực tế trong DB
        var adminRole = await roleManager.FindByNameAsync("Admin");
        if (adminRole == null)
        {
            Console.WriteLine("❌ Admin role not found!");
            return;
        }

        // ← Query lại từ DB sau khi đã SaveChanges ở bước trước
        var commandInFunctions = await context.CommandInFunctions.ToListAsync();

        if (!commandInFunctions.Any())
        {
            Console.WriteLine("❌ CommandInFunctions is empty, cannot seed Permissions!");
            return;
        }

        var permissions = commandInFunctions.Select(cif => new Permission
        {
            FunctionId = cif.FunctionId,
            CommandId = cif.CommandId,
            RoleId = adminRole.Id, // ← Id thực tế từ DB
        }).ToList();

        await context.Permissions.AddRangeAsync(permissions);

        Console.WriteLine($"✅ Seeded {permissions.Count} permissions for Admin role.");
    }
}