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
        await SeedUsersAsync(userManager);       // ← Gộp tất cả users vào 1 method

        await SeedFunctionsAsync(context);
        await context.SaveChangesAsync();

        await SeedCommandsAsync(context);
        await context.SaveChangesAsync();

        await SeedCommandInFunctionsAsync(context);
        await context.SaveChangesAsync();

        await SeedPermissionsAsync(context, roleManager);
        await context.SaveChangesAsync();
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
    // 2. ALL USERS — Admin, HR, Mentor, Intern
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedUsersAsync(UserManager<AppUser> userManager)
    {
        // ── Admin ─────────────────────────────────────────────
        if (await userManager.FindByEmailAsync("admin@deha.vn") == null)
        {
            var admin = new AppUser
            {
                Id = "admin-seed-001",
                UserName = "admin@deha.vn",
                Email = "admin@deha.vn",
                FirstName = "System",
                LastName = "Admin",
                IsActive = true,
                EmailConfirmed = true,
            };
            var result = await userManager.CreateAsync(admin, "Admin@2025!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }

        // ── HR ────────────────────────────────────────────────
        if (await userManager.FindByEmailAsync("hr@deha.vn") == null)
        {
            var hr = new AppUser
            {
                Id = "hr-seed-001",
                UserName = "hr@deha.vn",
                Email = "hr@deha.vn",
                FirstName = "Le",
                LastName = "Thi HR",
                IsActive = true,
                EmailConfirmed = true,
            };
            var result = await userManager.CreateAsync(hr, "Hr@2025!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(hr, "HR");
        }

        // ── Mentor ────────────────────────────────────────────
        if (await userManager.FindByEmailAsync("mentor@deha.vn") == null)
        {
            var mentor = new AppUser
            {
                Id = "mentor-seed-001",
                UserName = "mentor@deha.vn",
                Email = "mentor@deha.vn",
                FirstName = "Nguyen",
                LastName = "Van Mentor",
                IsActive = true,
                EmailConfirmed = true,
            };
            var result = await userManager.CreateAsync(mentor, "Mentor@2025!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(mentor, "Mentor");
        }

        // ── Intern ────────────────────────────────────────────
        if (await userManager.FindByEmailAsync("intern@deha.vn") == null)
        {
            var intern = new AppUser
            {
                Id = "intern-seed-001",
                UserName = "intern@deha.vn",
                Email = "intern@deha.vn",
                FirstName = "Tran",
                LastName = "Van Intern",
                IsActive = true,
                EmailConfirmed = true,
                StudentId = "SV001",
                GPA = 3.5m,
            };
            var result = await userManager.CreateAsync(intern, "Intern@2025!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(intern, "Intern");
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 3. FUNCTIONS
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedFunctionsAsync(AimsDbContext context)
    {
        if (await context.Functions.AnyAsync()) return;

        var functions = new List<Function>
        {
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

            new Function { Id = "RECRUITMENT_JD",
                           Name = "Job Descriptions",   Url = "/recruitment/jd",
                           Icon = "bi bi-file-text",    SortOrder = 1, ParentId = "RECRUITMENT" },
            new Function { Id = "RECRUITMENT_CV",
                           Name = "CV Screening (AI)",  Url = "/recruitment/screening",
                           Icon = "bi bi-robot",        SortOrder = 2, ParentId = "RECRUITMENT" },
            new Function { Id = "RECRUITMENT_RANKING",
                           Name = "Ranking ứng viên",   Url = "/recruitment/ranking",
                           Icon = "bi bi-trophy",       SortOrder = 3, ParentId = "RECRUITMENT" },

            new Function { Id = "LMS_COURSES",
                           Name = "Khóa học",           Url = "/lms/courses",
                           Icon = "bi bi-collection",   SortOrder = 1, ParentId = "LMS" },
            new Function { Id = "LMS_QUIZ",
                           Name = "Bài kiểm tra",       Url = "/lms/quiz",
                           Icon = "bi bi-pencil-square",SortOrder = 2, ParentId = "LMS" },
            new Function { Id = "LMS_CERTIFICATE",
                           Name = "Chứng chỉ",          Url = "/lms/certificates",
                           Icon = "bi bi-award",        SortOrder = 3, ParentId = "LMS" },

            new Function { Id = "TASKS_BOARD",
                           Name = "Kanban Board",       Url = "/tasks/board",
                           Icon = "bi bi-columns-gap",  SortOrder = 1, ParentId = "TASKS" },
            new Function { Id = "TASKS_REPORT",
                           Name = "Daily Report",       Url = "/tasks/daily-report",
                           Icon = "bi bi-journal-text", SortOrder = 2, ParentId = "TASKS" },
            new Function { Id = "TASKS_TIMESHEET",
                           Name = "Timesheet",          Url = "/tasks/timesheet",
                           Icon = "bi bi-clock",        SortOrder = 3, ParentId = "TASKS" },

            new Function { Id = "SYSTEM_USER",
                           Name = "Quản lý User",       Url = "/system/users",
                           Icon = "bi bi-people",       SortOrder = 1, ParentId = "SYSTEM" },
            new Function { Id = "SYSTEM_ROLE",
                           Name = "Quản lý Role",       Url = "/system/roles",
                           Icon = "bi bi-shield",       SortOrder = 2, ParentId = "SYSTEM" },
            new Function { Id = "SYSTEM_PERMISSION",
                           Name = "Phân quyền",         Url = "/system/permissions",
                           Icon = "bi bi-key",          SortOrder = 3, ParentId = "SYSTEM" },
        };

        await context.Functions.AddRangeAsync(functions);
    }

    // ─────────────────────────────────────────────────────────────
    // 4. COMMANDS
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
    // 5. COMMAND IN FUNCTIONS
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedCommandInFunctionsAsync(AimsDbContext context)
    {
        if (await context.CommandInFunctions.AnyAsync()) return;

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

    // ─────────────────────────────────────────────────────────────
    // 6. PERMISSIONS — Admin + HR + Mentor + Intern
    // ─────────────────────────────────────────────────────────────
    private static async Task SeedPermissionsAsync(
        AimsDbContext context,
        RoleManager<AppRole> roleManager)
    {
        if (await context.Permissions.AnyAsync()) return;

        var permissions = new List<Permission>();

        // ── ADMIN — toàn quyền ────────────────────────────────
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

        // ── HR — tuyển dụng ───────────────────────────────────
        var hrRole = await roleManager.FindByNameAsync("HR");
        if (hrRole != null)
        {
            var hrPerms = new[]
            {
                ("DASHBOARD",            "VIEW"),
                ("RECRUITMENT",          "VIEW"),
                ("RECRUITMENT_JD",       "VIEW"),
                ("RECRUITMENT_JD",       "CREATE"),
                ("RECRUITMENT_JD",       "UPDATE"),
                ("RECRUITMENT_CV",       "VIEW"),
                ("RECRUITMENT_CV",       "APPROVE"),
                ("RECRUITMENT_RANKING",  "VIEW"),
                ("RECRUITMENT_RANKING",  "EXPORT"),
                ("REPORTS",              "VIEW"),
            };
            permissions.AddRange(hrPerms.Select(p => new Permission
            {
                FunctionId = p.Item1,
                CommandId = p.Item2,
                RoleId = hrRole.Id,
            }));
        }

        // ── MENTOR — đào tạo + task ───────────────────────────
        var mentorRole = await roleManager.FindByNameAsync("Mentor");
        if (mentorRole != null)
        {
            var mentorPerms = new[]
            {
                ("DASHBOARD",        "VIEW"),
                ("LMS",              "VIEW"),
                ("LMS_COURSES",      "VIEW"),
                ("LMS_COURSES",      "CREATE"),
                ("LMS_COURSES",      "UPDATE"),
                ("LMS_COURSES",      "DELETE"),
                ("LMS_QUIZ",         "VIEW"),
                ("LMS_QUIZ",         "CREATE"),
                ("LMS_QUIZ",         "UPDATE"),
                ("LMS_QUIZ",         "DELETE"),
                ("LMS_CERTIFICATE",  "VIEW"),
                ("TASKS",            "VIEW"),
                ("TASKS_BOARD",      "VIEW"),
                ("TASKS_BOARD",      "CREATE"),
                ("TASKS_BOARD",      "UPDATE"),
                ("TASKS_REPORT",     "VIEW"),
                ("TASKS_REPORT",     "UPDATE"),
                ("TASKS_REPORT",     "APPROVE"),
                ("REPORTS",          "VIEW"),
            };
            permissions.AddRange(mentorPerms.Select(p => new Permission
            {
                FunctionId = p.Item1,
                CommandId = p.Item2,
                RoleId = mentorRole.Id,
            }));
        }

        // ── INTERN — học + làm bài + báo cáo ─────────────────
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
        Console.WriteLine($"✅ Seeded {permissions.Count} permissions for all roles.");
    }
}