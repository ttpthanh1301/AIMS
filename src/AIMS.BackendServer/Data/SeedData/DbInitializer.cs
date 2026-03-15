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
        // Users/Roles chỉ seed 1 lần — có guard là đúng
        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(userManager);

        // ⭐ Permission data — LUÔN reset và seed lại
        await ResetAndSeedPermissionDataAsync(context, roleManager);
    }

    // ─────────────────────────────────────────────────────────
    // RESET + SEED — Xóa sạch rồi seed lại
    // ─────────────────────────────────────────────────────────
    private static async Task ResetAndSeedPermissionDataAsync(
        AimsDbContext context,
        RoleManager<AppRole> roleManager)
    {
        // Xóa theo đúng thứ tự FK
        context.Permissions.RemoveRange(context.Permissions);
        context.CommandInFunctions.RemoveRange(context.CommandInFunctions);
        context.Commands.RemoveRange(context.Commands);
        context.Functions.RemoveRange(context.Functions);
        await context.SaveChangesAsync();

        Console.WriteLine("🗑️  Đã xóa data cũ");

        await SeedFunctionsAsync(context);
        await context.SaveChangesAsync();
        Console.WriteLine("✅ Functions seeded");

        await SeedCommandsAsync(context);
        await context.SaveChangesAsync();
        Console.WriteLine("✅ Commands seeded");

        await SeedCommandInFunctionsAsync(context);
        await context.SaveChangesAsync();
        Console.WriteLine("✅ CommandInFunctions seeded");

        await SeedPermissionsAsync(context, roleManager);
        await context.SaveChangesAsync();
        Console.WriteLine("✅ Permissions seeded");
    }

    // ─────────────────────────────────────────────────────────
    // 1. ROLES — giữ guard (không tạo lại role đã có)
    // ─────────────────────────────────────────────────────────
    private static async Task SeedRolesAsync(RoleManager<AppRole> roleManager)
    {
        var roles = new List<AppRole>
        {
            new() { Id = "admin",  Name = "Admin",  NormalizedName = "ADMIN",
                    Description = "Quản trị viên hệ thống, toàn quyền" },
            new() { Id = "hr",     Name = "HR",     NormalizedName = "HR",
                    Description = "Nhân viên tuyển dụng" },
            new() { Id = "mentor", Name = "Mentor", NormalizedName = "MENTOR",
                    Description = "Người hướng dẫn thực tập sinh" },
            new() { Id = "intern", Name = "Intern", NormalizedName = "INTERN",
                    Description = "Thực tập sinh" },
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name!))
                await roleManager.CreateAsync(role);
        }
    }

    // ─────────────────────────────────────────────────────────
    // 2. USERS — giữ guard (không tạo lại user đã có)
    // ─────────────────────────────────────────────────────────
    private static async Task SeedUsersAsync(UserManager<AppUser> userManager)
    {
        var users = new[]
        {
            (Email: "admin@deha.vn",  Pass: "Admin@2025!",  Role: "Admin",
             Id: "admin-seed-001",  First: "System",  Last: "Admin",
             StudentId: (string?)null, GPA: (decimal?)null),

            (Email: "hr@deha.vn",     Pass: "Hr@2025!",     Role: "HR",
             Id: "hr-seed-001",     First: "Le",      Last: "Thi HR",
             StudentId: (string?)null, GPA: (decimal?)null),

            (Email: "mentor@deha.vn", Pass: "Mentor@2025!", Role: "Mentor",
             Id: "mentor-seed-001", First: "Nguyen",  Last: "Van Mentor",
             StudentId: (string?)null, GPA: (decimal?)null),

            (Email: "intern@deha.vn", Pass: "Intern@2025!", Role: "Intern",
             Id: "intern-seed-001", First: "Tran",    Last: "Van Intern",
             StudentId: (string?)"SV001", GPA: (decimal?)3.5m),
        };

        foreach (var u in users)
        {
            if (await userManager.FindByEmailAsync(u.Email) != null) continue;

            var user = new AppUser
            {
                Id = u.Id,
                UserName = u.Email,
                Email = u.Email,
                FirstName = u.First,
                LastName = u.Last,
                IsActive = true,
                EmailConfirmed = true,
                StudentId = u.StudentId,
                GPA = u.GPA,
            };

            var result = await userManager.CreateAsync(user, u.Pass);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, u.Role);
        }
    }

    // ─────────────────────────────────────────────────────────
    // 3. FUNCTIONS — KHÔNG có guard
    // ─────────────────────────────────────────────────────────
    private static async Task SeedFunctionsAsync(AimsDbContext context)
    {
        // ⭐ KHÔNG có if (AnyAsync()) return
        var functions = new List<Function>
        {
            new() { Id = "DASHBOARD",   Name = "Dashboard",
                    Url = "/dashboard", Icon = "bi bi-speedometer2",
                    SortOrder = 1, ParentId = null },
            new() { Id = "RECRUITMENT", Name = "Tuyển dụng",
                    Url = "/recruitment", Icon = "bi bi-person-plus",
                    SortOrder = 2, ParentId = null },
            new() { Id = "LMS",         Name = "Đào tạo",
                    Url = "/lms", Icon = "bi bi-book",
                    SortOrder = 3, ParentId = null },
            new() { Id = "TASKS",       Name = "Quản lý Task",
                    Url = "/tasks", Icon = "bi bi-kanban",
                    SortOrder = 4, ParentId = null },
            new() { Id = "REPORTS",     Name = "Báo cáo",
                    Url = "/reports", Icon = "bi bi-bar-chart",
                    SortOrder = 5, ParentId = null },
            new() { Id = "SYSTEM",      Name = "Hệ thống",
                    Url = "/system", Icon = "bi bi-gear",
                    SortOrder = 6, ParentId = null },

            new() { Id = "RECRUITMENT_JD",
                    Name = "Job Descriptions",  Url = "/recruitment/jd",
                    Icon = "bi bi-file-text",   SortOrder = 1,
                    ParentId = "RECRUITMENT" },
            new() { Id = "RECRUITMENT_CV",
                    Name = "CV Screening (AI)", Url = "/recruitment/screening",
                    Icon = "bi bi-robot",       SortOrder = 2,
                    ParentId = "RECRUITMENT" },
            new() { Id = "RECRUITMENT_RANKING",
                    Name = "Ranking ứng viên",  Url = "/recruitment/ranking",
                    Icon = "bi bi-trophy",      SortOrder = 3,
                    ParentId = "RECRUITMENT" },

            new() { Id = "LMS_COURSES",
                    Name = "Khóa học",          Url = "/lms/courses",
                    Icon = "bi bi-collection",  SortOrder = 1,
                    ParentId = "LMS" },
            new() { Id = "LMS_QUIZ",
                    Name = "Bài kiểm tra",      Url = "/lms/quiz",
                    Icon = "bi bi-pencil-square",SortOrder = 2,
                    ParentId = "LMS" },
            new() { Id = "LMS_CERTIFICATE",
                    Name = "Chứng chỉ",         Url = "/lms/certificates",
                    Icon = "bi bi-award",       SortOrder = 3,
                    ParentId = "LMS" },

            new() { Id = "TASKS_BOARD",
                    Name = "Kanban Board",      Url = "/tasks/board",
                    Icon = "bi bi-columns-gap", SortOrder = 1,
                    ParentId = "TASKS" },
            new() { Id = "TASKS_REPORT",
                    Name = "Daily Report",      Url = "/tasks/daily-report",
                    Icon = "bi bi-journal-text",SortOrder = 2,
                    ParentId = "TASKS" },
            new() { Id = "TASKS_TIMESHEET",
                    Name = "Timesheet",         Url = "/tasks/timesheet",
                    Icon = "bi bi-clock",       SortOrder = 3,
                    ParentId = "TASKS" },

            new() { Id = "SYSTEM_USER",
                    Name = "Quản lý User",      Url = "/system/users",
                    Icon = "bi bi-people",      SortOrder = 1,
                    ParentId = "SYSTEM" },
            new() { Id = "SYSTEM_ROLE",
                    Name = "Quản lý Role",      Url = "/system/roles",
                    Icon = "bi bi-shield",      SortOrder = 2,
                    ParentId = "SYSTEM" },
            new() { Id = "SYSTEM_PERMISSION",
                    Name = "Phân quyền",        Url = "/system/permissions",
                    Icon = "bi bi-key",         SortOrder = 3,
                    ParentId = "SYSTEM" },
        };

        await context.Functions.AddRangeAsync(functions);
    }

    // ─────────────────────────────────────────────────────────
    // 4. COMMANDS — KHÔNG có guard
    // ─────────────────────────────────────────────────────────
    private static async Task SeedCommandsAsync(AimsDbContext context)
    {
        // ⭐ KHÔNG có if (AnyAsync()) return
        var commands = new List<Command>
        {
            new() { Id = "VIEW",    Name = "Xem" },
            new() { Id = "CREATE",  Name = "Tạo mới" },
            new() { Id = "UPDATE",  Name = "Cập nhật" },
            new() { Id = "DELETE",  Name = "Xóa" },
            new() { Id = "EXPORT",  Name = "Xuất dữ liệu" },
            new() { Id = "IMPORT",  Name = "Nhập dữ liệu" },
            new() { Id = "APPROVE", Name = "Duyệt / Phê duyệt" },
        };

        await context.Commands.AddRangeAsync(commands);
    }

    // ─────────────────────────────────────────────────────────
    // 5. COMMAND IN FUNCTIONS — KHÔNG có guard
    // ─────────────────────────────────────────────────────────
    private static async Task SeedCommandInFunctionsAsync(AimsDbContext context)
    {
        // ⭐ KHÔNG có if (AnyAsync()) return
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

        foreach (var fId in allFuncIds)
            cifs.Add(new CommandInFunction { CommandId = "VIEW", FunctionId = fId });

        foreach (var fId in crudFuncIds)
        {
            cifs.Add(new CommandInFunction { CommandId = "CREATE", FunctionId = fId });
            cifs.Add(new CommandInFunction { CommandId = "UPDATE", FunctionId = fId });
            cifs.Add(new CommandInFunction { CommandId = "DELETE", FunctionId = fId });
        }

        cifs.Add(new CommandInFunction { CommandId = "EXPORT", FunctionId = "REPORTS" });
        cifs.Add(new CommandInFunction { CommandId = "EXPORT", FunctionId = "RECRUITMENT_RANKING" });
        cifs.Add(new CommandInFunction { CommandId = "APPROVE", FunctionId = "RECRUITMENT_CV" });
        cifs.Add(new CommandInFunction { CommandId = "APPROVE", FunctionId = "TASKS_REPORT" });

        await context.CommandInFunctions.AddRangeAsync(cifs);
    }

    // ─────────────────────────────────────────────────────────
    // 6. PERMISSIONS — KHÔNG có guard
    // ─────────────────────────────────────────────────────────
    private static async Task SeedPermissionsAsync(
        AimsDbContext context,
        RoleManager<AppRole> roleManager)
    {
        // ⭐ KHÔNG có if (AnyAsync()) return
        var permissions = new List<Permission>();

        // Admin — toàn quyền
        var adminRole = await roleManager.FindByNameAsync("Admin");
        if (adminRole != null)
        {
            var allCifs = await context.CommandInFunctions.ToListAsync();
            permissions.AddRange(allCifs.Select(cif => new Permission
            {
                FunctionId = cif.FunctionId,
                CommandId = cif.CommandId,
                RoleId = adminRole.Id,
            }));
        }

        // HR
        var hrRole = await roleManager.FindByNameAsync("HR");
        if (hrRole != null)
        {
            var hrPerms = new[]
            {
                ("DASHBOARD",           "VIEW"),
                ("RECRUITMENT",         "VIEW"),
                ("RECRUITMENT_JD",      "VIEW"),
                ("RECRUITMENT_JD",      "CREATE"),
                ("RECRUITMENT_JD",      "UPDATE"),
                ("RECRUITMENT_CV",      "VIEW"),
                ("RECRUITMENT_CV",      "APPROVE"),
                ("RECRUITMENT_RANKING", "VIEW"),
                ("RECRUITMENT_RANKING", "EXPORT"),
                ("REPORTS",             "VIEW"),
            };
            permissions.AddRange(hrPerms.Select(p => new Permission
            {
                FunctionId = p.Item1,
                CommandId = p.Item2,
                RoleId = hrRole.Id,
            }));
        }

        // Mentor
        var mentorRole = await roleManager.FindByNameAsync("Mentor");
        if (mentorRole != null)
        {
            var mentorPerms = new[]
            {
                ("DASHBOARD",       "VIEW"),
                ("LMS",             "VIEW"),
                ("LMS_COURSES",     "VIEW"),
                ("LMS_COURSES",     "CREATE"),
                ("LMS_COURSES",     "UPDATE"),
                ("LMS_COURSES",     "DELETE"),
                ("LMS_QUIZ",        "VIEW"),
                ("LMS_QUIZ",        "CREATE"),
                ("LMS_QUIZ",        "UPDATE"),
                ("LMS_QUIZ",        "DELETE"),
                ("LMS_CERTIFICATE", "VIEW"),
                ("TASKS",           "VIEW"),
                ("TASKS_BOARD",     "VIEW"),
                ("TASKS_BOARD",     "CREATE"),   // ← Tạo task
                ("TASKS_BOARD",     "UPDATE"),   // ← Sửa task
                ("TASKS_BOARD",     "DELETE"),   // ← Xóa task
                ("TASKS_REPORT",    "VIEW"),
                ("TASKS_REPORT",    "UPDATE"),
                ("TASKS_REPORT",    "APPROVE"),
                ("REPORTS",         "VIEW"),
            };
            permissions.AddRange(mentorPerms.Select(p => new Permission
            {
                FunctionId = p.Item1,
                CommandId = p.Item2,
                RoleId = mentorRole.Id,
            }));
        }

        // Intern
        var internRole = await roleManager.FindByNameAsync("Intern");
        if (internRole != null)
        {
            var internPerms = new[]
            {
                ("DASHBOARD",        "VIEW"),
                ("LMS",              "VIEW"),
                ("LMS_COURSES",      "VIEW"),
                ("LMS_QUIZ",         "VIEW"),
                ("LMS_QUIZ",         "CREATE"),
                ("LMS_CERTIFICATE",  "VIEW"),
                ("TASKS",            "VIEW"),
                ("TASKS_BOARD",      "VIEW"),
                ("TASKS_BOARD",      "UPDATE"),  // ← Đổi status task
                ("TASKS_REPORT",     "VIEW"),
                ("TASKS_REPORT",     "CREATE"),
                ("TASKS_REPORT",     "UPDATE"),
                ("TASKS_TIMESHEET",  "VIEW"),
                ("TASKS_TIMESHEET",  "CREATE"),
            };
            permissions.AddRange(internPerms.Select(p => new Permission
            {
                FunctionId = p.Item1,
                CommandId = p.Item2,
                RoleId = internRole.Id,
            }));
        }

        await context.Permissions.AddRangeAsync(permissions);
        Console.WriteLine($"✅ Seeded {permissions.Count} permissions.");
    }
}