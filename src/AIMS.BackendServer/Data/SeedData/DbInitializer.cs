using AIMS.BackendServer.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AIMS.BackendServer.Data.SeedData;

/// <summary>
/// Khởi tạo dữ liệu mẫu cho hệ thống AIMS.
///
/// Chiến lược 2 bước:
///   1. C# (Identity)  → Tạo 4 role + 4 user thực với password hash đúng.
///                        Id phải khớp với SQL bên dưới để MERGE không tạo bản ghi trùng.
///   2. SQL (MERGE)     → Upsert toàn bộ dữ liệu bulk (30 intern, courses, tasks...).
///                        Chạy lại nhiều lần không bị lỗi duplicate.
/// </summary>
public static class DbInitializer
{
    // ═══════════════════════════════════════════════════════════
    // Public entry point
    // ═══════════════════════════════════════════════════════════

    public static async Task SeedAsync(
        AimsDbContext context,
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager)
    {
        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(userManager);
        await context.Database.ExecuteSqlRawAsync(FullScaleSeedSql);
    }

    // ═══════════════════════════════════════════════════════════
    // Bước 1a — Seed 4 roles cơ bản qua Identity
    //
    // ⚠️ Id ở đây PHẢI khớp với giá trị Id trong SQL MERGE bên dưới
    //    để tránh tạo bản ghi trùng NormalizedName.
    // ═══════════════════════════════════════════════════════════

    private static async Task SeedRolesAsync(RoleManager<AppRole> roleManager)
    {
        var roles = new[]
        {
            new AppRole { Id = "ROLE_ADMIN",  Name = "Admin",  NormalizedName = "ADMIN",  Description = "Quản trị viên hệ thống, toàn quyền" },
            new AppRole { Id = "ROLE_HR",     Name = "HR",     NormalizedName = "HR",     Description = "Nhân viên tuyển dụng" },
            new AppRole { Id = "ROLE_MENTOR", Name = "Mentor", NormalizedName = "MENTOR", Description = "Người hướng dẫn thực tập sinh" },
            new AppRole { Id = "ROLE_INTERN", Name = "Intern", NormalizedName = "INTERN", Description = "Thực tập sinh" },
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name!))
            {
                var result = await roleManager.CreateAsync(role);
                if (!result.Succeeded)
                    throw new InvalidOperationException(
                        $"Cannot create role '{role.Name}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Bước 1b — Seed 4 users thực với password hash đúng
    //
    // ⚠️ Id + Email + UserName ở đây PHẢI khớp với SQL bên dưới.
    //    SQL MERGE dùng ON T.[Id] = S.[Id] → nếu Id đã tồn tại
    //    (do C# tạo), MERGE sẽ bỏ qua (WHEN NOT MATCHED BY TARGET).
    //
    //    Mapping với SQL:
    //      "U_ADMIN_001"  ↔ admin@deha.vn   (SQL row U_ADMIN_001)
    //      "U_HR_001"     ↔ hr.minh@deha.vn (SQL row U_HR_001)
    //      "U_MENTOR_001" ↔ hoang@deha.vn   (SQL row U_MENTOR_001)
    //      "U_INTERN_001" ↔ thanh@sv.vn     (SQL row U_INTERN_001)
    // ═══════════════════════════════════════════════════════════

    private static async Task SeedUsersAsync(UserManager<AppUser> userManager)
    {
        // Mật khẩu dùng chung theo từng role
        const string AdminPassword = "Admin@2025!";
        const string HrPassword = "Hr@2025!";
        const string MentorPassword = "Mentor@2025!";
        const string InternPassword = "Intern@2025!";

        var users = new[]
        {
            (Id: "U_ADMIN_001", Email: "admin@deha.vn", UserName: "admin", Pass: AdminPassword, Role: "Admin", First: "Admin", Last: "System", StudentId: (string?)null, GPA: (decimal?)null),
            (Id: "U_HR_001", Email: "hr.minh@deha.vn", UserName: "hr.minh", Pass: HrPassword, Role: "HR", First: "Minh", Last: "Nguyễn HR", StudentId: (string?)null, GPA: (decimal?)null),
            (Id: "U_HR_002", Email: "hr.lan@deha.vn", UserName: "hr.lan", Pass: HrPassword, Role: "HR", First: "Lan", Last: "Phạm HR", StudentId: (string?)null, GPA: (decimal?)null),
            (Id: "U_MENTOR_001", Email: "hoang@deha.vn", UserName: "mentor.hoang", Pass: MentorPassword, Role: "Mentor", First: "Hoàng", Last: "Backend", StudentId: (string?)null, GPA: (decimal?)null),
            (Id: "U_MENTOR_002", Email: "anh@deha.vn", UserName: "mentor.anh", Pass: MentorPassword, Role: "Mentor", First: "Anh", Last: "AI_NLP", StudentId: (string?)null, GPA: (decimal?)null),
            (Id: "U_MENTOR_003", Email: "duc@deha.vn", UserName: "mentor.duc", Pass: MentorPassword, Role: "Mentor", First: "Đức", Last: "QA_QC", StudentId: (string?)null, GPA: (decimal?)null),
            (Id: "U_MENTOR_004", Email: "huong@deha.vn", UserName: "mentor.huong", Pass: MentorPassword, Role: "Mentor", First: "Hương", Last: "BA_Lead", StudentId: (string?)null, GPA: (decimal?)null),
            (Id: "U_INTERN_001", Email: "thanh@sv.vn", UserName: "intern.thanh", Pass: InternPassword, Role: "Intern", First: "Thanh", Last: "Trần Phương", StudentId: (string?)"SV001", GPA: (decimal?)3.85m),
            (Id: "U_INTERN_002", Email: "nam@sv.vn", UserName: "intern.nam", Pass: InternPassword, Role: "Intern", First: "Nam", Last: "Nguyễn Hoài", StudentId: (string?)"SV002", GPA: (decimal?)3.20m),
            (Id: "U_INTERN_003", Email: "linh@sv.vn", UserName: "intern.linh", Pass: InternPassword, Role: "Intern", First: "Linh", Last: "Vũ Khánh", StudentId: (string?)"SV003", GPA: (decimal?)3.72m),
            (Id: "U_INTERN_004", Email: "dung@sv.vn", UserName: "intern.dung", Pass: InternPassword, Role: "Intern", First: "Dũng", Last: "Phạm Minh", StudentId: (string?)"SV004", GPA: (decimal?)2.95m),
            (Id: "U_INTERN_005", Email: "ha@sv.vn", UserName: "intern.ha", Pass: InternPassword, Role: "Intern", First: "Hà", Last: "Đỗ Ngọc", StudentId: (string?)"SV005", GPA: (decimal?)3.10m),
            (Id: "U_INTERN_006", Email: "quan@sv.vn", UserName: "intern.quan", Pass: InternPassword, Role: "Intern", First: "Quân", Last: "Bùi Anh", StudentId: (string?)"SV006", GPA: (decimal?)3.60m),
            (Id: "U_INTERN_007", Email: "hai@sv.vn", UserName: "intern.hai", Pass: InternPassword, Role: "Intern", First: "Hải", Last: "Trần Ngọc", StudentId: (string?)"SV007", GPA: (decimal?)3.50m),
            (Id: "U_INTERN_008", Email: "thao@sv.vn", UserName: "intern.thao", Pass: InternPassword, Role: "Intern", First: "Thảo", Last: "Lê Phương", StudentId: (string?)"SV008", GPA: (decimal?)3.65m),
            (Id: "U_INTERN_009", Email: "phong@sv.vn", UserName: "intern.phong", Pass: InternPassword, Role: "Intern", First: "Phong", Last: "Nguyễn Đình", StudentId: (string?)"SV009", GPA: (decimal?)3.15m),
            (Id: "U_INTERN_010", Email: "yen@sv.vn", UserName: "intern.yen", Pass: InternPassword, Role: "Intern", First: "Yến", Last: "Hoàng Hải", StudentId: (string?)"SV010", GPA: (decimal?)3.80m),
            (Id: "U_INTERN_011", Email: "cuong@sv.vn", UserName: "intern.cuong", Pass: InternPassword, Role: "Intern", First: "Cường", Last: "Vũ Quốc", StudentId: (string?)"SV011", GPA: (decimal?)3.00m),
            (Id: "U_INTERN_012", Email: "mai@sv.vn", UserName: "intern.mai", Pass: InternPassword, Role: "Intern", First: "Mai", Last: "Đặng Phương", StudentId: (string?)"SV012", GPA: (decimal?)3.40m),
            (Id: "U_INTERN_013", Email: "tuan@sv.vn", UserName: "intern.tuan", Pass: InternPassword, Role: "Intern", First: "Tuấn", Last: "Đinh Khắc", StudentId: (string?)"SV013", GPA: (decimal?)2.80m),
            (Id: "U_INTERN_014", Email: "trang@sv.vn", UserName: "intern.trang", Pass: InternPassword, Role: "Intern", First: "Trang", Last: "Lý Thu", StudentId: (string?)"SV014", GPA: (decimal?)3.55m),
            (Id: "U_INTERN_015", Email: "khoa@sv.vn", UserName: "intern.khoa", Pass: InternPassword, Role: "Intern", First: "Khoa", Last: "Hồ Đăng", StudentId: (string?)"SV015", GPA: (decimal?)3.70m),
            (Id: "U_INTERN_016", Email: "binh@sv.vn", UserName: "intern.binh", Pass: InternPassword, Role: "Intern", First: "Bình", Last: "Trương Thanh", StudentId: (string?)"SV016", GPA: (decimal?)3.10m),
            (Id: "U_INTERN_017", Email: "hoa@sv.vn", UserName: "intern.hoa", Pass: InternPassword, Role: "Intern", First: "Hoa", Last: "Ngô Quý", StudentId: (string?)"SV017", GPA: (decimal?)3.25m),
            (Id: "U_INTERN_018", Email: "viet@sv.vn", UserName: "intern.viet", Pass: InternPassword, Role: "Intern", First: "Việt", Last: "Bùi Quang", StudentId: (string?)"SV018", GPA: (decimal?)2.90m),
            (Id: "U_INTERN_019", Email: "giang@sv.vn", UserName: "intern.giang", Pass: InternPassword, Role: "Intern", First: "Giang", Last: "Phạm Hương", StudentId: (string?)"SV019", GPA: (decimal?)3.65m),
            (Id: "U_INTERN_020", Email: "dat@sv.vn", UserName: "intern.dat", Pass: InternPassword, Role: "Intern", First: "Đạt", Last: "Nguyễn Thành", StudentId: (string?)"SV020", GPA: (decimal?)3.80m),
            (Id: "U_INTERN_021", Email: "my@sv.vn", UserName: "intern.my", Pass: InternPassword, Role: "Intern", First: "My", Last: "Trần Trà", StudentId: (string?)"SV021", GPA: (decimal?)3.40m),
            (Id: "U_INTERN_022", Email: "long@sv.vn", UserName: "intern.long", Pass: InternPassword, Role: "Intern", First: "Long", Last: "Hoàng Phi", StudentId: (string?)"SV022", GPA: (decimal?)2.85m),
            (Id: "U_INTERN_023", Email: "han@sv.vn", UserName: "intern.han", Pass: InternPassword, Role: "Intern", First: "Hân", Last: "Đỗ Gia", StudentId: (string?)"SV023", GPA: (decimal?)3.90m),
            (Id: "U_INTERN_024", Email: "son@sv.vn", UserName: "intern.son", Pass: InternPassword, Role: "Intern", First: "Sơn", Last: "Lê Tùng", StudentId: (string?)"SV024", GPA: (decimal?)3.05m),
            (Id: "U_INTERN_025", Email: "an@sv.vn", UserName: "intern.an", Pass: InternPassword, Role: "Intern", First: "An", Last: "Vũ Bình", StudentId: (string?)"SV025", GPA: (decimal?)3.35m),
            (Id: "U_INTERN_026", Email: "phuc@sv.vn", UserName: "intern.phuc", Pass: InternPassword, Role: "Intern", First: "Phúc", Last: "Nguyễn Hồng", StudentId: (string?)"SV026", GPA: (decimal?)3.15m),
            (Id: "U_INTERN_027", Email: "tam@sv.vn", UserName: "intern.tam", Pass: InternPassword, Role: "Intern", First: "Tâm", Last: "Trần Minh", StudentId: (string?)"SV027", GPA: (decimal?)3.45m),
            (Id: "U_INTERN_028", Email: "khanh@sv.vn", UserName: "intern.khanh", Pass: InternPassword, Role: "Intern", First: "Khánh", Last: "Lý Quốc", StudentId: (string?)"SV028", GPA: (decimal?)2.75m),
            (Id: "U_INTERN_029", Email: "nhi@sv.vn", UserName: "intern.nhi", Pass: InternPassword, Role: "Intern", First: "Nhi", Last: "Đặng Yến", StudentId: (string?)"SV029", GPA: (decimal?)3.60m),
            (Id: "U_INTERN_030", Email: "bao@sv.vn", UserName: "intern.bao", Pass: InternPassword, Role: "Intern", First: "Bảo", Last: "Phạm Gia", StudentId: (string?)"SV030", GPA: (decimal?)3.85m),
        };

        foreach (var u in users)
        {
            var user = await userManager.FindByIdAsync(u.Id)
                    ?? await userManager.FindByEmailAsync(u.Email);

            if (user == null)
            {
                user = new AppUser
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    NormalizedUserName = u.UserName.ToUpperInvariant(),
                    Email = u.Email,
                    NormalizedEmail = u.Email.ToUpperInvariant(),
                    FirstName = u.First,
                    LastName = u.Last,
                    IsActive = true,
                    EmailConfirmed = true,
                    StudentId = u.StudentId,
                    GPA = u.GPA,
                    CreateDate = DateTime.UtcNow,
                };

                var createResult = await userManager.CreateAsync(user, u.Pass);
                if (!createResult.Succeeded)
                    throw new InvalidOperationException(
                        $"Cannot create user '{u.Email}': {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }
            else
            {
                // Reset mật khẩu mỗi lần seed để các account demo luôn đăng nhập được.
                user.UserName = u.UserName;
                user.NormalizedUserName = u.UserName.ToUpperInvariant();
                user.Email = u.Email;
                user.NormalizedEmail = u.Email.ToUpperInvariant();
                user.FirstName = u.First;
                user.LastName = u.Last;
                user.IsActive = true;
                user.EmailConfirmed = true;
                user.StudentId = u.StudentId;
                user.GPA = u.GPA;
                user.PasswordHash = userManager.PasswordHasher.HashPassword(user, u.Pass);

                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                    throw new InvalidOperationException(
                        $"Cannot update user '{u.Email}': {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");

                await userManager.UpdateSecurityStampAsync(user);
            }

            if (!await userManager.IsInRoleAsync(user, u.Role))
            {
                var roleResult = await userManager.AddToRoleAsync(user, u.Role);
                if (!roleResult.Succeeded)
                    throw new InvalidOperationException(
                        $"Cannot add '{u.Email}' to role '{u.Role}': {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Bước 2 — Bulk seed toàn bộ dữ liệu qua SQL (UPSERT an toàn)
    // ═══════════════════════════════════════════════════════════

    private const string FullScaleSeedSql = """
        -- ╔══════════════════════════════════════════════════════════╗
        -- ║  AIMS FULL SCALE SEED DATA — SAFE UPSERT / NO DELETE    ║
        -- ║  Chạy lại nhiều lần không lỗi duplicate key             ║
        -- ╚══════════════════════════════════════════════════════════╝

        -- Cleanup IDENTITY_INSERT state nếu session trước bị crash giữa chừng
        DECLARE @IdentityTables TABLE (TableName sysname);
        INSERT INTO @IdentityTables (TableName) VALUES
            (N'Universities'), (N'JobPositions'), (N'JobDescriptions'),
            (N'Applications'), (N'CVParsedDatas'), (N'AIScreeningResults'),
            (N'Courses'), (N'CourseChapters'), (N'Lessons'),
            (N'InternshipPeriods');

        DECLARE @T sysname, @Sql nvarchar(max);
        DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT TableName FROM @IdentityTables;
        OPEN cur; FETCH NEXT FROM cur INTO @T;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            BEGIN TRY
                SET @Sql = N'SET IDENTITY_INSERT dbo.' + QUOTENAME(@T) + N' OFF;';
                EXEC sp_executesql @Sql;
            END TRY BEGIN CATCH END CATCH;
            FETCH NEXT FROM cur INTO @T;
        END
        CLOSE cur; DEALLOCATE cur;

        SET NOCOUNT ON;
        SET XACT_ABORT ON;

        DECLARE @SeedDate datetime2 = CONVERT(date, GETDATE());
        DECLARE @DemoReportDate datetime2 = DATEADD(DAY, -1, @SeedDate);

        BEGIN TRY
            BEGIN TRANSACTION;

            ------------------------------------------------------------
            -- UPSERT AppRoles
            -- ⚠️ Id PHẢI khớp với SeedRolesAsync() trong C# ở trên.
            --    MERGE dùng ON NormalizedName nên nếu C# đã tạo role
            --    với Id='ROLE_ADMIN', MERGE sẽ UPDATE chứ không INSERT mới.
            ------------------------------------------------------------
            ;WITH Source ([Id], [Description], [Name], [NormalizedName], [ConcurrencyStamp]) AS
            (
                SELECT * FROM (VALUES
                    (N'ROLE_ADMIN',  N'Quản trị viên hệ thống, toàn quyền', N'Admin',  N'ADMIN',  NEWID()),
                    (N'ROLE_HR',     N'Nhân viên tuyển dụng',                N'HR',     N'HR',     NEWID()),
                    (N'ROLE_MENTOR', N'Người hướng dẫn thực tập sinh',       N'Mentor', N'MENTOR', NEWID()),
                    (N'ROLE_INTERN', N'Thực tập sinh',                        N'Intern', N'INTERN', NEWID())
                ) AS V ([Id], [Description], [Name], [NormalizedName], [ConcurrencyStamp])
            )
            MERGE [AppRoles] AS T
            USING Source AS S ON T.[NormalizedName] = S.[NormalizedName]
            WHEN MATCHED THEN
                UPDATE SET T.[Description] = S.[Description], T.[Name] = S.[Name]
            WHEN NOT MATCHED BY TARGET THEN
                INSERT ([Id], [Description], [Name], [NormalizedName], [ConcurrencyStamp])
                VALUES (S.[Id], S.[Description], S.[Name], S.[NormalizedName], S.[ConcurrencyStamp]);

            ------------------------------------------------------------
            -- UPSERT Commands
            ------------------------------------------------------------
            ;WITH Source ([Id], [Name]) AS
            (
                SELECT * FROM (VALUES
                    (N'VIEW',      N'Xem'),
                    (N'CREATE',    N'Tạo'),
                    (N'UPDATE',    N'Sửa'),
                    (N'DELETE',    N'Xóa'),
                    (N'APPROVE',   N'Duyệt'),
                    (N'IMPORT_CV', N'Import CV'),
                    (N'AI_SCREEN', N'Sàng lọc AI'),
                    (N'EXPORT',    N'Xuất DB')
                ) AS V ([Id], [Name])
            )
            MERGE [Commands] AS T
            USING Source AS S ON T.[Id] = S.[Id]
            WHEN NOT MATCHED BY TARGET THEN
                INSERT ([Id], [Name]) VALUES (S.[Id], S.[Name]);

            ------------------------------------------------------------
            -- UPSERT Functions
            ------------------------------------------------------------
            ;WITH Source ([Id], [Name], [Url], [Icon], [SortOrder], [ParentId]) AS
            (
                SELECT * FROM (VALUES
                    (N'DASHBOARD',       N'Tổng quan',    N'/dashboard',                       N'fa fa-chart-line',      1, NULL),
                    (N'RECRUITMENT',     N'Tuyển dụng',   N'/recruitment',                     N'fa fa-user-check',      2, NULL),
                    (N'LMS',             N'Đào tạo',      N'/lms',                             N'fa fa-graduation-cap',  3, NULL),
                    (N'TASK_MANAGEMENT', N'Thực tập',     N'/tasks',                           N'fa fa-tasks',           4, NULL),
                    (N'REPORT',          N'Báo cáo',      N'/reports',                         N'fa fa-file-alt',        5, NULL),
                    (N'CV_SCREENING',    N'Sàng lọc AI',  N'/recruitment/cv-screening',        N'fa fa-robot',           1, N'RECRUITMENT'),
                    (N'JOB_DESCRIPTION', N'Mô tả JD',     N'/recruitment/job-descriptions',    N'fa fa-briefcase',       2, N'RECRUITMENT'),
                    (N'COURSE',          N'Khóa học',     N'/lms/courses',                     N'fa fa-book',            1, N'LMS'),
                    (N'INTERN_TASK',     N'Nhiệm vụ',     N'/tasks/intern-tasks',              N'fa fa-list-check',      1, N'TASK_MANAGEMENT'),
                    (N'DAILY_REPORT',    N'Báo cáo ngày', N'/tasks/daily-reports',             N'fa fa-pen',             2, N'TASK_MANAGEMENT'),
                    (N'TIMESHEET',       N'Chấm công',    N'/tasks/timesheets',                N'fa fa-clock',           3, N'TASK_MANAGEMENT')
                ) AS V ([Id], [Name], [Url], [Icon], [SortOrder], [ParentId])
            )
            MERGE [Functions] AS T
            USING Source AS S ON T.[Id] = S.[Id]
            WHEN NOT MATCHED BY TARGET THEN
                INSERT ([Id], [Name], [Url], [Icon], [SortOrder], [ParentId])
                VALUES (S.[Id], S.[Name], S.[Url], S.[Icon], S.[SortOrder], S.[ParentId]);

            ------------------------------------------------------------
            -- UPSERT CommandInFunctions
            ------------------------------------------------------------
            INSERT INTO [CommandInFunctions] ([CommandId], [FunctionId])
            SELECT c.Id, f.Id
            FROM [Commands] c CROSS JOIN [Functions] f
            WHERE c.Id IN (N'VIEW', N'CREATE', N'UPDATE', N'DELETE', N'EXPORT')
              AND NOT EXISTS (
                    SELECT 1 FROM [CommandInFunctions] cif
                    WHERE cif.[CommandId] = c.[Id] AND cif.[FunctionId] = f.[Id]);

            ------------------------------------------------------------
            -- UPSERT Permissions cho Admin
            -- Dùng Id thực của role ADMIN từ bảng AppRoles (không hardcode)
            ------------------------------------------------------------
            INSERT INTO [Permissions] ([FunctionId], [RoleId], [CommandId])
            SELECT cif.FunctionId, r.Id, cif.CommandId
            FROM [CommandInFunctions] cif
            CROSS JOIN [AppRoles] r
            WHERE r.NormalizedName = N'ADMIN'
              AND NOT EXISTS (
                    SELECT 1 FROM [Permissions] p
                    WHERE p.[FunctionId] = cif.[FunctionId]
                      AND p.[RoleId]     = r.[Id]
                      AND p.[CommandId]  = cif.[CommandId]);

            ------------------------------------------------------------
            -- UPSERT Permissions cho Mentor / Intern
            -- Khớp với PermissionMiddleware:
            --   /api/tasks        -> INTERN_TASK
            --   /api/dailyreports -> DAILY_REPORT
            ------------------------------------------------------------
            ;WITH RolePermissionSource ([RoleName], [FunctionId], [CommandId]) AS
            (
                SELECT * FROM (VALUES
                    (N'MENTOR', N'INTERN_TASK',  N'VIEW'),
                    (N'MENTOR', N'INTERN_TASK',  N'CREATE'),
                    (N'MENTOR', N'INTERN_TASK',  N'UPDATE'),
                    (N'MENTOR', N'INTERN_TASK',  N'DELETE'),
                    (N'MENTOR', N'DAILY_REPORT', N'VIEW'),
                    (N'MENTOR', N'DAILY_REPORT', N'UPDATE'),
                    (N'MENTOR', N'JOB_DESCRIPTION', N'VIEW'),
                    (N'MENTOR', N'JOB_DESCRIPTION', N'CREATE'),
                    (N'MENTOR', N'JOB_DESCRIPTION', N'UPDATE'),
                    (N'MENTOR', N'CV_SCREENING',    N'VIEW'),
                    (N'MENTOR', N'CV_SCREENING',    N'CREATE'),
                    (N'HR',     N'JOB_DESCRIPTION', N'VIEW'),
                    (N'HR',     N'JOB_DESCRIPTION', N'CREATE'),
                    (N'HR',     N'JOB_DESCRIPTION', N'UPDATE'),
                    (N'HR',     N'JOB_DESCRIPTION', N'DELETE'),
                    (N'HR',     N'CV_SCREENING',    N'VIEW'),
                    (N'HR',     N'CV_SCREENING',    N'CREATE'),
                    (N'HR',     N'CV_SCREENING',    N'UPDATE'),
                    (N'HR',     N'CV_SCREENING',    N'DELETE'),
                    (N'INTERN', N'INTERN_TASK',  N'VIEW'),
                    (N'INTERN', N'INTERN_TASK',  N'UPDATE'),
                    (N'INTERN', N'DAILY_REPORT', N'VIEW'),
                    (N'INTERN', N'DAILY_REPORT', N'CREATE')
                ) AS V ([RoleName], [FunctionId], [CommandId])
            )
            INSERT INTO [Permissions] ([FunctionId], [RoleId], [CommandId])
            SELECT rps.FunctionId, r.Id, rps.CommandId
            FROM RolePermissionSource rps
            INNER JOIN [AppRoles] r ON r.NormalizedName = rps.RoleName
            WHERE NOT EXISTS (
                SELECT 1 FROM [Permissions] p
                WHERE p.[FunctionId] = rps.[FunctionId]
                  AND p.[RoleId]     = r.[Id]
                  AND p.[CommandId]  = rps.[CommandId]);

            ------------------------------------------------------------
            -- UPSERT Universities
            ------------------------------------------------------------
            SET IDENTITY_INSERT [Universities] ON;
            ;WITH Source ([Id], [Name], [City]) AS
            (
                SELECT * FROM (VALUES
                    (1,  N'Đại học Bách khoa Hà Nội',      N'Hà Nội'),
                    (2,  N'Đại học Công nghệ - ĐHQGHN',    N'Hà Nội'),
                    (3,  N'Học viện Công nghệ BCVT',        N'Hà Nội'),
                    (4,  N'Đại học FPT',                    N'Hà Nội'),
                    (5,  N'Đại học KHTN - ĐHQG TP.HCM',    N'TP.HCM'),
                    (6,  N'Đại học Duy Tân',                N'Đà Nẵng'),
                    (7,  N'Đại học Thương Mại (TMU)',       N'Hà Nội'),
                    (8,  N'Đại học Kinh tế Quốc dân',      N'Hà Nội'),
                    (9,  N'Đại học Giao thông Vận tải',    N'Hà Nội'),
                    (10, N'Học viện Ngân hàng',             N'Hà Nội'),
                    (11, N'Học viện Kỹ thuật Mật mã',      N'Hà Nội'),
                    (12, N'Đại học Công nghiệp Hà Nội',    N'Hà Nội'),
                    (13, N'Đại học Sư phạm Kỹ thuật',      N'TP.HCM'),
                    (14, N'Đại học Bách khoa TP.HCM',      N'TP.HCM'),
                    (15, N'Đại học Tôn Đức Thắng',         N'TP.HCM')
                ) AS V ([Id], [Name], [City])
            )
            MERGE [Universities] AS T
            USING Source AS S ON T.[Id] = S.[Id]
            WHEN NOT MATCHED BY TARGET THEN
                INSERT ([Id], [Name], [City]) VALUES (S.[Id], S.[Name], S.[City]);
            SET IDENTITY_INSERT [Universities] OFF;

            ------------------------------------------------------------
            -- UPSERT AppUsers (bulk — không có password hash thực)
            --
            -- ⚠️ 4 user đã được C# tạo ở bước trên (U_ADMIN_001, U_HR_001,
            --    U_MENTOR_001, U_INTERN_001) với password hash đúng.
            --    MERGE dùng ON T.[Id] = S.[Id] → những user đó đã tồn tại
            --    → chỉ bị UPDATE các trường phi-security, KHÔNG đụng PasswordHash.
            --
            --    Các user còn lại (U_HR_002, U_MENTOR_002..4, U_INTERN_002..030)
            --    được INSERT mới với PasswordHash = N'HASH' (dữ liệu demo).
            ------------------------------------------------------------
            ;WITH Source ([Id], [FirstName], [LastName], [IsActive], [CreateDate],
                          [UserName], [NormalizedUserName], [Email], [NormalizedEmail],
                          [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp],
                          [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled],
                          [AccessFailedCount], [UniversityId], [GPA]) AS
            (
                SELECT * FROM (VALUES
                    -- ── Admin ──────────────────────────────────────────────────────────────
                    (N'U_ADMIN_001',  N'Admin',  N'System',        1, GETDATE(), N'admin',         N'ADMIN',         N'admin@deha.vn',    N'ADMIN@DEHA.VN',    1, N'HASH', NEWID(), NEWID(), 1,0,1,0, NULL, NULL),
                    -- ── HR ─────────────────────────────────────────────────────────────────
                    (N'U_HR_001',     N'Minh',   N'Nguyễn HR',     1, GETDATE(), N'hr.minh',       N'HR.MINH',       N'hr.minh@deha.vn',  N'HR.MINH@DEHA.VN',  1, N'HASH', NEWID(), NEWID(), 1,0,1,0, NULL, NULL),
                    (N'U_HR_002',     N'Lan',    N'Phạm HR',       1, GETDATE(), N'hr.lan',        N'HR.LAN',        N'hr.lan@deha.vn',   N'HR.LAN@DEHA.VN',   1, N'HASH', NEWID(), NEWID(), 1,0,1,0, NULL, NULL),
                    -- ── Mentor ─────────────────────────────────────────────────────────────
                    (N'U_MENTOR_001', N'Hoàng',  N'Backend',       1, GETDATE(), N'mentor.hoang',  N'MENTOR.HOANG',  N'hoang@deha.vn',    N'HOANG@DEHA.VN',    1, N'HASH', NEWID(), NEWID(), 1,0,1,0, NULL, NULL),
                    (N'U_MENTOR_002', N'Anh',    N'AI_NLP',        1, GETDATE(), N'mentor.anh',    N'MENTOR.ANH',    N'anh@deha.vn',      N'ANH@DEHA.VN',      1, N'HASH', NEWID(), NEWID(), 1,0,1,0, NULL, NULL),
                    (N'U_MENTOR_003', N'Đức',    N'QA_QC',         1, GETDATE(), N'mentor.duc',    N'MENTOR.DUC',    N'duc@deha.vn',      N'DUC@DEHA.VN',      1, N'HASH', NEWID(), NEWID(), 1,0,1,0, NULL, NULL),
                    (N'U_MENTOR_004', N'Hương',  N'BA_Lead',       1, GETDATE(), N'mentor.huong',  N'MENTOR.HUONG',  N'huong@deha.vn',    N'HUONG@DEHA.VN',    1, N'HASH', NEWID(), NEWID(), 1,0,1,0, NULL, NULL),
                    -- ── Intern ─────────────────────────────────────────────────────────────
                    (N'U_INTERN_001', N'Thanh',  N'Trần Phương',   1, GETDATE(), N'intern.thanh',  N'INTERN.THANH',  N'thanh@sv.vn',      N'THANH@SV.VN',      1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 7,  3.85),
                    (N'U_INTERN_002', N'Nam',    N'Nguyễn Hoài',   1, GETDATE(), N'intern.nam',    N'INTERN.NAM',    N'nam@sv.vn',        N'NAM@SV.VN',        1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 2,  3.20),
                    (N'U_INTERN_003', N'Linh',   N'Vũ Khánh',      1, GETDATE(), N'intern.linh',   N'INTERN.LINH',   N'linh@sv.vn',       N'LINH@SV.VN',       1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 3,  3.72),
                    (N'U_INTERN_004', N'Dũng',   N'Phạm Minh',     1, GETDATE(), N'intern.dung',   N'INTERN.DUNG',   N'dung@sv.vn',       N'DUNG@SV.VN',       1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 4,  2.95),
                    (N'U_INTERN_005', N'Hà',     N'Đỗ Ngọc',       1, GETDATE(), N'intern.ha',     N'INTERN.HA',     N'ha@sv.vn',         N'HA@SV.VN',         1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 5,  3.10),
                    (N'U_INTERN_006', N'Quân',   N'Bùi Anh',       1, GETDATE(), N'intern.quan',   N'INTERN.QUAN',   N'quan@sv.vn',       N'QUAN@SV.VN',       1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 6,  3.60),
                    (N'U_INTERN_007', N'Hải',    N'Trần Ngọc',     1, GETDATE(), N'intern.hai',    N'INTERN.HAI',    N'hai@sv.vn',        N'HAI@SV.VN',        1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 7,  3.50),
                    (N'U_INTERN_008', N'Thảo',   N'Lê Phương',     1, GETDATE(), N'intern.thao',   N'INTERN.THAO',   N'thao@sv.vn',       N'THAO@SV.VN',       1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 8,  3.65),
                    (N'U_INTERN_009', N'Phong',  N'Nguyễn Đình',   1, GETDATE(), N'intern.phong',  N'INTERN.PHONG',  N'phong@sv.vn',      N'PHONG@SV.VN',      1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 1,  3.15),
                    (N'U_INTERN_010', N'Yến',    N'Hoàng Hải',     1, GETDATE(), N'intern.yen',    N'INTERN.YEN',    N'yen@sv.vn',        N'YEN@SV.VN',        1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 7,  3.80),
                    (N'U_INTERN_011', N'Cường',  N'Vũ Quốc',       1, GETDATE(), N'intern.cuong',  N'INTERN.CUONG',  N'cuong@sv.vn',      N'CUONG@SV.VN',      1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 9,  3.00),
                    (N'U_INTERN_012', N'Mai',    N'Đặng Phương',   1, GETDATE(), N'intern.mai',    N'INTERN.MAI',    N'mai@sv.vn',        N'MAI@SV.VN',        1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 10, 3.40),
                    (N'U_INTERN_013', N'Tuấn',   N'Đinh Khắc',     1, GETDATE(), N'intern.tuan',   N'INTERN.TUAN',   N'tuan@sv.vn',       N'TUAN@SV.VN',       1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 11, 2.80),
                    (N'U_INTERN_014', N'Trang',  N'Lý Thu',        1, GETDATE(), N'intern.trang',  N'INTERN.TRANG',  N'trang@sv.vn',      N'TRANG@SV.VN',      1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 12, 3.55),
                    (N'U_INTERN_015', N'Khoa',   N'Hồ Đăng',       1, GETDATE(), N'intern.khoa',   N'INTERN.KHOA',   N'khoa@sv.vn',       N'KHOA@SV.VN',       1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 13, 3.70),
                    (N'U_INTERN_016', N'Bình',   N'Trương Thanh',  1, GETDATE(), N'intern.binh',   N'INTERN.BINH',   N'binh@sv.vn',       N'BINH@SV.VN',       1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 14, 3.10),
                    (N'U_INTERN_017', N'Hoa',    N'Ngô Quý',       1, GETDATE(), N'intern.hoa',    N'INTERN.HOA',    N'hoa@sv.vn',        N'HOA@SV.VN',        1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 15, 3.25),
                    (N'U_INTERN_018', N'Việt',   N'Bùi Quang',     1, GETDATE(), N'intern.viet',   N'INTERN.VIET',   N'viet@sv.vn',       N'VIET@SV.VN',       1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 1,  2.90),
                    (N'U_INTERN_019', N'Giang',  N'Phạm Hương',    1, GETDATE(), N'intern.giang',  N'INTERN.GIANG',  N'giang@sv.vn',      N'GIANG@SV.VN',      1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 2,  3.65),
                    (N'U_INTERN_020', N'Đạt',    N'Nguyễn Thành',  1, GETDATE(), N'intern.dat',    N'INTERN.DAT',    N'dat@sv.vn',        N'DAT@SV.VN',        1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 3,  3.80),
                    (N'U_INTERN_021', N'My',     N'Trần Trà',      1, GETDATE(), N'intern.my',     N'INTERN.MY',     N'my@sv.vn',         N'MY@SV.VN',         1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 4,  3.40),
                    (N'U_INTERN_022', N'Long',   N'Hoàng Phi',     1, GETDATE(), N'intern.long',   N'INTERN.LONG',   N'long@sv.vn',       N'LONG@SV.VN',       1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 5,  2.85),
                    (N'U_INTERN_023', N'Hân',    N'Đỗ Gia',        1, GETDATE(), N'intern.han',    N'INTERN.HAN',    N'han@sv.vn',        N'HAN@SV.VN',        1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 6,  3.90),
                    (N'U_INTERN_024', N'Sơn',    N'Lê Tùng',       1, GETDATE(), N'intern.son',    N'INTERN.SON',    N'son@sv.vn',        N'SON@SV.VN',        1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 7,  3.05),
                    (N'U_INTERN_025', N'An',     N'Vũ Bình',       1, GETDATE(), N'intern.an',     N'INTERN.AN',     N'an@sv.vn',         N'AN@SV.VN',         1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 8,  3.35),
                    (N'U_INTERN_026', N'Phúc',   N'Nguyễn Hồng',  1, GETDATE(), N'intern.phuc',   N'INTERN.PHUC',   N'phuc@sv.vn',       N'PHUC@SV.VN',       1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 9,  3.15),
                    (N'U_INTERN_027', N'Tâm',    N'Trần Minh',     1, GETDATE(), N'intern.tam',    N'INTERN.TAM',    N'tam@sv.vn',        N'TAM@SV.VN',        1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 10, 3.45),
                    (N'U_INTERN_028', N'Khánh',  N'Lý Quốc',      1, GETDATE(), N'intern.khanh',  N'INTERN.KHANH',  N'khanh@sv.vn',      N'KHANH@SV.VN',      1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 11, 2.75),
                    (N'U_INTERN_029', N'Nhi',    N'Đặng Yến',      1, GETDATE(), N'intern.nhi',    N'INTERN.NHI',    N'nhi@sv.vn',        N'NHI@SV.VN',        1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 12, 3.60),
                    (N'U_INTERN_030', N'Bảo',    N'Phạm Gia',      1, GETDATE(), N'intern.bao',    N'INTERN.BAO',    N'bao@sv.vn',        N'BAO@SV.VN',        1, N'HASH', NEWID(), NEWID(), 1,0,1,0, 13, 3.85)
                ) AS V ([Id], [FirstName], [LastName], [IsActive], [CreateDate],
                         [UserName], [NormalizedUserName], [Email], [NormalizedEmail],
                         [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp],
                         [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled],
                         [AccessFailedCount], [UniversityId], [GPA])
            )
            MERGE [AppUsers] AS T
            USING Source AS S ON T.[Id] = S.[Id]
            WHEN MATCHED THEN
                -- Chỉ UPDATE các trường phi-security. KHÔNG đụng PasswordHash của 4 user thực.
                UPDATE SET
                    T.[FirstName]          = S.[FirstName],
                    T.[LastName]           = S.[LastName],
                    T.[IsActive]           = S.[IsActive],
                    T.[UniversityId]       = S.[UniversityId],
                    T.[GPA]                = S.[GPA]
            WHEN NOT MATCHED BY TARGET THEN
                INSERT ([Id], [FirstName], [LastName], [IsActive], [CreateDate],
                        [UserName], [NormalizedUserName], [Email], [NormalizedEmail],
                        [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp],
                        [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled],
                        [AccessFailedCount], [UniversityId], [GPA])
                VALUES (S.[Id], S.[FirstName], S.[LastName], S.[IsActive], S.[CreateDate],
                        S.[UserName], S.[NormalizedUserName], S.[Email], S.[NormalizedEmail],
                        S.[EmailConfirmed], S.[PasswordHash], S.[SecurityStamp], S.[ConcurrencyStamp],
                        S.[PhoneNumberConfirmed], S.[TwoFactorEnabled], S.[LockoutEnabled],
                        S.[AccessFailedCount], S.[UniversityId], S.[GPA]);

            ------------------------------------------------------------
            -- UPSERT UserRoles
            -- Mapping theo prefix Id của AppUsers → NormalizedName của AppRoles
            -- NOT EXISTS đảm bảo không insert trùng (kể cả 4 user đã gán qua C#)
            ------------------------------------------------------------
            ;WITH UserRoleMap AS
            (
                SELECT u.Id AS UserId, r.Id AS RoleId
                FROM [AppUsers] u
                INNER JOIN [AppRoles] r ON r.[NormalizedName] =
                    CASE
                        WHEN u.Id LIKE N'U_ADMIN%'  THEN N'ADMIN'
                        WHEN u.Id LIKE N'U_HR%'     THEN N'HR'
                        WHEN u.Id LIKE N'U_MENTOR%' THEN N'MENTOR'
                        WHEN u.Id LIKE N'U_INTERN%' THEN N'INTERN'
                    END
                WHERE u.Id LIKE N'U_%'
            )
            INSERT INTO [UserRoles] ([UserId], [RoleId])
            SELECT m.UserId, m.RoleId
            FROM UserRoleMap m
            WHERE NOT EXISTS (
                SELECT 1 FROM [UserRoles] ur
                WHERE ur.[UserId] = m.[UserId] AND ur.[RoleId] = m.[RoleId]);

            ------------------------------------------------------------
            -- UPSERT JobPositions
            ------------------------------------------------------------
            SET IDENTITY_INSERT [JobPositions] ON;
            ;WITH Source ([Id], [Title], [Description], [IsActive], [CreateDate]) AS
            (
                SELECT * FROM (VALUES
                    (1,  N'.NET Backend',     N'API, EF Core',             1, GETDATE()),
                    (2,  N'Frontend',         N'Web MVC, React',           1, GETDATE()),
                    (3,  N'AI/NLP',           N'Xử lý văn bản, TF-IDF',   1, GETDATE()),
                    (4,  N'QA/QC',            N'Test Case, k6',            1, GETDATE()),
                    (5,  N'Business Analyst', N'FRS, Use Case',            1, GETDATE()),
                    (6,  N'Mobile App',       N'React Native, Flutter',    1, GETDATE()),
                    (7,  N'Data Analyst',     N'SQL, PowerBI',             1, GETDATE()),
                    (8,  N'DevOps',           N'CI/CD, Docker, AWS',       1, GETDATE()),
                    (9,  N'UI/UX Design',     N'Figma, Prototype',         1, GETDATE()),
                    (10, N'NodeJS Backend',   N'Express, MongoDB',         1, GETDATE())
                ) AS V ([Id], [Title], [Description], [IsActive], [CreateDate])
            )
            MERGE [JobPositions] AS T
            USING Source AS S ON T.[Id] = S.[Id]
            WHEN NOT MATCHED BY TARGET THEN
                INSERT ([Id], [Title], [Description], [IsActive], [CreateDate])
                VALUES (S.[Id], S.[Title], S.[Description], S.[IsActive], S.[CreateDate]);
            SET IDENTITY_INSERT [JobPositions] OFF;

            ------------------------------------------------------------
            -- UPSERT JobDescriptions
            ------------------------------------------------------------
            SET IDENTITY_INSERT [JobDescriptions] ON;
            ;WITH Source ([Id], [JobPositionId], [Title], [DetailContent], [RequiredSkills], [MinGPA], [CreatedByUserId], [Status], [CreateDate], [DeadlineDate]) AS
            (
                SELECT * FROM (VALUES
                    (1,  1, N'JD .NET Backend - Đợt 1',  N'Phát triển API bằng ASP.NET Core.',            N'C#, ASP.NET Core, SQL Server', 3.0, N'U_HR_001', N'OPEN',   GETDATE(), DATEADD(DAY,  30, GETDATE())),
                    (2,  2, N'JD Frontend Web - Đợt 1',  N'Thiết kế giao diện trên nền tảng Web.',        N'HTML, CSS, JS, React',         2.8, N'U_HR_001', N'OPEN',   GETDATE(), DATEADD(DAY,  30, GETDATE())),
                    (3,  3, N'JD AI/NLP - Đợt 1',        N'Nghiên cứu mô hình bóc tách dữ liệu văn bản.',N'Python, ML.NET, NLP',          3.2, N'U_HR_002', N'OPEN',   GETDATE(), DATEADD(DAY,  45, GETDATE())),
                    (4,  4, N'JD Tester/QA - Đợt 1',     N'Viết kịch bản test, chạy script hiệu năng.',  N'Manual Test, API Test, k6',    2.8, N'U_HR_001', N'OPEN',   GETDATE(), DATEADD(DAY,  30, GETDATE())),
                    (5,  5, N'JD BA Intern - Đợt 1',     N'Lấy yêu cầu khách hàng, viết FRS.',           N'UML, FRS, Use Case',           3.0, N'U_HR_002', N'OPEN',   GETDATE(), DATEADD(DAY,  30, GETDATE())),
                    (6,  6, N'JD Mobile React Native',    N'Build ứng dụng mobile đa nền tảng.',          N'React Native, JS/TS',          2.8, N'U_HR_001', N'OPEN',   GETDATE(), DATEADD(DAY,  30, GETDATE())),
                    (7,  7, N'JD Data Analyst Intern',    N'Xử lý số liệu, làm Dashboard thống kê.',      N'SQL, Python, PowerBI',         3.2, N'U_HR_002', N'OPEN',   GETDATE(), DATEADD(DAY,  30, GETDATE())),
                    (8,  8, N'JD DevOps Intern',          N'Cấu hình môi trường, đẩy Docker lên AWS.',    N'Linux, Docker, AWS ECR',       3.0, N'U_HR_001', N'OPEN',   GETDATE(), DATEADD(DAY,  30, GETDATE())),
                    (9,  9, N'JD UI/UX Intern',           N'Làm Wireframe, Prototype cho module LMS.',    N'Figma, UI/UX Principles',      2.8, N'U_HR_002', N'OPEN',   GETDATE(), DATEADD(DAY,  30, GETDATE())),
                    (10,10, N'JD NodeJS Backend',         N'Phát triển API bằng NodeJS.',                 N'NodeJS, Express, MongoDB',     3.0, N'U_HR_001', N'OPEN',   GETDATE(), DATEADD(DAY,  30, GETDATE())),
                    (11, 1, N'JD .NET Backend - Đợt 2',  N'Xây dựng Microservices với C#.',              N'C#, Microservices, Docker',    3.2, N'U_HR_001', N'OPEN',   GETDATE(), DATEADD(DAY,  30, GETDATE())),
                    (12, 5, N'JD BA Intern - Đợt 2',     N'Tối ưu quy trình tuyển dụng thông minh.',    N'BPMN, SQL Cơ bản, FRS',        3.0, N'U_HR_002', N'OPEN',   GETDATE(), DATEADD(DAY,  30, GETDATE())),
                    (13, 4, N'JD Automation QA',          N'Làm Automation test bằng C# và Selenium.',   N'Selenium, k6, C#',             3.0, N'U_HR_001', N'OPEN',   GETDATE(), DATEADD(DAY,  30, GETDATE())),
                    (14, 2, N'JD Frontend VueJS',         N'Dùng VueJS làm giao diện Dashboard.',        N'VueJS, TailwindCSS',           2.8, N'U_HR_001', N'CLOSED', GETDATE(), DATEADD(DAY,  -5, GETDATE())),
                    (15, 8, N'JD Cloud/DevOps',           N'Triển khai dự án lên nền tảng Azure.',       N'Azure, Kubernetes',            3.5, N'U_HR_002', N'CLOSED', GETDATE(), DATEADD(DAY,  -5, GETDATE()))
                ) AS V ([Id], [JobPositionId], [Title], [DetailContent], [RequiredSkills], [MinGPA], [CreatedByUserId], [Status], [CreateDate], [DeadlineDate])
            )
            MERGE [JobDescriptions] AS T
            USING Source AS S ON T.[Id] = S.[Id]
            WHEN NOT MATCHED BY TARGET THEN
                INSERT ([Id], [JobPositionId], [Title], [DetailContent], [RequiredSkills], [MinGPA], [CreatedByUserId], [Status], [CreateDate], [DeadlineDate])
                VALUES (S.[Id], S.[JobPositionId], S.[Title], S.[DetailContent], S.[RequiredSkills], S.[MinGPA], S.[CreatedByUserId], S.[Status], S.[CreateDate], S.[DeadlineDate]);
            SET IDENTITY_INSERT [JobDescriptions] OFF;

            ------------------------------------------------------------
            -- UPSERT Applications
            ------------------------------------------------------------
            SET IDENTITY_INSERT [Applications] ON;
            ;WITH Source ([Id], [ApplicantUserId], [JobDescriptionId], [CVFileUrl], [CoverLetter], [ApplyDate], [Status]) AS
            (
                SELECT * FROM (VALUES
                    (1,  N'U_INTERN_001', 1,  N'/cv/1.pdf',  N'Xin ứng tuyển vào vị trí Backend.',      DATEADD(DAY,-15,GETDATE()), N'ACCEPTED'),
                    (2,  N'U_INTERN_002', 1,  N'/cv/2.pdf',  N'Xin ứng tuyển vào vị trí Backend.',      DATEADD(DAY,-14,GETDATE()), N'INTERVIEW'),
                    (3,  N'U_INTERN_003', 3,  N'/cv/3.pdf',  N'Xin ứng tuyển vào vị trí AI NLP.',       DATEADD(DAY,-13,GETDATE()), N'ACCEPTED'),
                    (4,  N'U_INTERN_004', 2,  N'/cv/4.pdf',  N'Xin ứng tuyển vào vị trí Frontend.',     DATEADD(DAY,-12,GETDATE()), N'PENDING'),
                    (5,  N'U_INTERN_005', 2,  N'/cv/5.pdf',  N'Xin ứng tuyển vào vị trí Frontend.',     DATEADD(DAY,-11,GETDATE()), N'REJECTED'),
                    (6,  N'U_INTERN_006', 3,  N'/cv/6.pdf',  N'Xin ứng tuyển vào vị trí AI NLP.',       DATEADD(DAY,-10,GETDATE()), N'SCREENING'),
                    (7,  N'U_INTERN_007', 4,  N'/cv/7.pdf',  N'Xin ứng tuyển vào vị trí QA/QC.',        DATEADD(DAY,-10,GETDATE()), N'ACCEPTED'),
                    (8,  N'U_INTERN_008', 5,  N'/cv/8.pdf',  N'Xin ứng tuyển vào vị trí BA.',           DATEADD(DAY, -9,GETDATE()), N'ACCEPTED'),
                    (9,  N'U_INTERN_009', 1,  N'/cv/9.pdf',  N'Xin ứng tuyển vào vị trí Backend.',      DATEADD(DAY, -8,GETDATE()), N'SCREENING'),
                    (10, N'U_INTERN_010', 5,  N'/cv/10.pdf', N'Xin ứng tuyển vào vị trí BA.',           DATEADD(DAY, -7,GETDATE()), N'INTERVIEW'),
                    (11, N'U_INTERN_011', 6,  N'/cv/11.pdf', N'Xin ứng tuyển vào vị trí Mobile.',       DATEADD(DAY, -6,GETDATE()), N'ACCEPTED'),
                    (12, N'U_INTERN_012', 7,  N'/cv/12.pdf', N'Xin ứng tuyển vào vị trí Data.',         DATEADD(DAY, -5,GETDATE()), N'PENDING'),
                    (13, N'U_INTERN_013', 8,  N'/cv/13.pdf', N'Xin ứng tuyển vào vị trí DevOps.',       DATEADD(DAY, -4,GETDATE()), N'INTERVIEW'),
                    (14, N'U_INTERN_014', 9,  N'/cv/14.pdf', N'Xin ứng tuyển vào vị trí UI/UX.',        DATEADD(DAY, -3,GETDATE()), N'ACCEPTED'),
                    (15, N'U_INTERN_015', 10, N'/cv/15.pdf', N'Xin ứng tuyển vào vị trí NodeJS.',       DATEADD(DAY, -2,GETDATE()), N'ACCEPTED'),
                    (16, N'U_INTERN_016', 1,  N'/cv/16.pdf', N'Xin ứng tuyển vào vị trí Backend.',      DATEADD(DAY,-15,GETDATE()), N'REJECTED'),
                    (17, N'U_INTERN_017', 2,  N'/cv/17.pdf', N'Xin ứng tuyển vào vị trí Frontend.',     DATEADD(DAY,-14,GETDATE()), N'SCREENING'),
                    (18, N'U_INTERN_018', 3,  N'/cv/18.pdf', N'Xin ứng tuyển vào vị trí AI NLP.',       DATEADD(DAY,-13,GETDATE()), N'ACCEPTED'),
                    (19, N'U_INTERN_019', 4,  N'/cv/19.pdf', N'Xin ứng tuyển vào vị trí QA/QC.',        DATEADD(DAY,-12,GETDATE()), N'PENDING'),
                    (20, N'U_INTERN_020', 5,  N'/cv/20.pdf', N'Xin ứng tuyển vào vị trí BA.',           DATEADD(DAY,-11,GETDATE()), N'INTERVIEW'),
                    (21, N'U_INTERN_021', 6,  N'/cv/21.pdf', N'Xin ứng tuyển vào vị trí Mobile.',       DATEADD(DAY,-10,GETDATE()), N'ACCEPTED'),
                    (22, N'U_INTERN_022', 7,  N'/cv/22.pdf', N'Xin ứng tuyển vào vị trí Data.',         DATEADD(DAY, -9,GETDATE()), N'REJECTED'),
                    (23, N'U_INTERN_023', 8,  N'/cv/23.pdf', N'Xin ứng tuyển vào vị trí DevOps.',       DATEADD(DAY, -8,GETDATE()), N'ACCEPTED'),
                    (24, N'U_INTERN_024', 9,  N'/cv/24.pdf', N'Xin ứng tuyển vào vị trí UI/UX.',        DATEADD(DAY, -7,GETDATE()), N'SCREENING'),
                    (25, N'U_INTERN_025', 10, N'/cv/25.pdf', N'Xin ứng tuyển vào vị trí NodeJS.',       DATEADD(DAY, -6,GETDATE()), N'PENDING'),
                    (26, N'U_INTERN_026', 11, N'/cv/26.pdf', N'Xin ứng tuyển Backend đợt 2.',           DATEADD(DAY, -5,GETDATE()), N'ACCEPTED'),
                    (27, N'U_INTERN_027', 12, N'/cv/27.pdf', N'Xin ứng tuyển BA đợt 2.',                DATEADD(DAY, -4,GETDATE()), N'INTERVIEW'),
                    (28, N'U_INTERN_028', 13, N'/cv/28.pdf', N'Xin ứng tuyển QA Automation.',           DATEADD(DAY, -3,GETDATE()), N'ACCEPTED'),
                    (29, N'U_INTERN_029', 4,  N'/cv/29.pdf', N'Xin ứng tuyển vào vị trí QA/QC.',        DATEADD(DAY, -2,GETDATE()), N'SCREENING'),
                    (30, N'U_INTERN_030', 5,  N'/cv/30.pdf', N'Xin ứng tuyển vào vị trí BA.',           DATEADD(DAY, -1,GETDATE()), N'ACCEPTED')
                ) AS V ([Id], [ApplicantUserId], [JobDescriptionId], [CVFileUrl], [CoverLetter], [ApplyDate], [Status])
            )
            MERGE [Applications] AS T
            USING Source AS S ON T.[Id] = S.[Id]
            WHEN NOT MATCHED BY TARGET THEN
                INSERT ([Id], [ApplicantUserId], [JobDescriptionId], [CVFileUrl], [CoverLetter], [ApplyDate], [Status])
                VALUES (S.[Id], S.[ApplicantUserId], S.[JobDescriptionId], S.[CVFileUrl], S.[CoverLetter], S.[ApplyDate], S.[Status]);
            SET IDENTITY_INSERT [Applications] OFF;

            ------------------------------------------------------------
            -- UPSERT CVParsedDatas
            ------------------------------------------------------------
            SET IDENTITY_INSERT [CVParsedDatas] ON;
            INSERT INTO [CVParsedDatas] ([Id], [ApplicationId], [FullName], [EmailExtracted], [SkillsExtracted], [RawText], [ParsedAt])
            SELECT a.Id, a.Id,
                N'Ứng viên ' + CAST(a.Id AS NVARCHAR(10)),
                N'uv' + CAST(a.Id AS NVARCHAR(10)) + N'@gmail.com',
                N'C#, SQL, Git, UML, Postman, k6',
                N'Dữ liệu CV bóc tách thô...', GETDATE()
            FROM [Applications] a
            WHERE NOT EXISTS (SELECT 1 FROM [CVParsedDatas] cv WHERE cv.[ApplicationId] = a.[Id]);
            SET IDENTITY_INSERT [CVParsedDatas] OFF;

            ------------------------------------------------------------
            -- UPSERT AIScreeningResults
            ------------------------------------------------------------
            SET IDENTITY_INSERT [AIScreeningResults] ON;
            INSERT INTO [AIScreeningResults] ([Id], [ApplicationId], [MatchingScore], [Ranking], [KeywordsMatched], [KeywordsMissing], [ProcessingStatus], [ScreenedAt], [ReviewedByHRId])
            SELECT a.Id, a.Id,
                CAST(60.0 + (a.Id % 40) AS decimal(5,2)),
                (a.Id % 3) + 1,
                N'C#, SQL, Git',
                N'Docker, Azure',
                N'Completed',
                GETDATE(),
                CASE WHEN a.Id % 2 = 0 THEN N'U_HR_001' ELSE N'U_HR_002' END
            FROM [Applications] a
            WHERE NOT EXISTS (SELECT 1 FROM [AIScreeningResults] ai WHERE ai.[ApplicationId] = a.[Id]);
            SET IDENTITY_INSERT [AIScreeningResults] OFF;

            ------------------------------------------------------------
            -- UPSERT Courses
            ------------------------------------------------------------
            SET IDENTITY_INSERT [Courses] ON;
            ;WITH Source ([Id], [Title], [Level], [CreatedByUserId], [IsPublished], [CreateDate]) AS
            (
                SELECT * FROM (VALUES
                    (1,  N'ASP.NET Core Web API',  N'BEGINNER',     N'U_MENTOR_001', 1, GETDATE()),
                    (2,  N'EF Core & SQL',          N'INTERMEDIATE', N'U_MENTOR_001', 1, GETDATE()),
                    (3,  N'NLP CV Screening',       N'INTERMEDIATE', N'U_MENTOR_002', 1, GETDATE()),
                    (4,  N'Docker & AWS ECR',       N'ADVANCED',     N'U_MENTOR_001', 1, GETDATE()),
                    (5,  N'Test k6 Performance',    N'INTERMEDIATE', N'U_MENTOR_003', 1, GETDATE()),
                    (6,  N'BA Thực chiến',          N'BEGINNER',     N'U_MENTOR_004', 1, GETDATE()),
                    (7,  N'React Native Cơ bản',    N'BEGINNER',     N'U_MENTOR_001', 1, GETDATE()),
                    (8,  N'PowerBI Dashboard',      N'INTERMEDIATE', N'U_MENTOR_002', 1, GETDATE()),
                    (9,  N'CI/CD Github Actions',   N'ADVANCED',     N'U_MENTOR_001', 1, GETDATE()),
                    (10, N'Agile Scrum 101',        N'BEGINNER',     N'U_MENTOR_004', 1, GETDATE())
                ) AS V ([Id], [Title], [Level], [CreatedByUserId], [IsPublished], [CreateDate])
            )
            MERGE [Courses] AS T
            USING Source AS S ON T.[Id] = S.[Id]
            WHEN NOT MATCHED BY TARGET THEN
                INSERT ([Id], [Title], [Level], [CreatedByUserId], [IsPublished], [CreateDate])
                VALUES (S.[Id], S.[Title], S.[Level], S.[CreatedByUserId], S.[IsPublished], S.[CreateDate]);
            SET IDENTITY_INSERT [Courses] OFF;

            ------------------------------------------------------------
            -- UPSERT CourseChapters
            ------------------------------------------------------------
            SET IDENTITY_INSERT [CourseChapters] ON;
            ;WITH Source ([Id], [CourseId], [Title], [SortOrder]) AS
            (
                SELECT * FROM (VALUES
                    (1,1,N'Chương 1',1),(2,2,N'Chương 1',1),(3,3,N'Chương 1',1),(4,4,N'Chương 1',1),(5,5,N'Chương 1',1),
                    (6,6,N'Chương 1',1),(7,7,N'Chương 1',1),(8,8,N'Chương 1',1),(9,9,N'Chương 1',1),(10,10,N'Chương 1',1)
                ) AS V ([Id], [CourseId], [Title], [SortOrder])
            )
            MERGE [CourseChapters] AS T
            USING Source AS S ON T.[Id] = S.[Id]
            WHEN NOT MATCHED BY TARGET THEN
                INSERT ([Id], [CourseId], [Title], [SortOrder])
                VALUES (S.[Id], S.[CourseId], S.[Title], S.[SortOrder]);
            SET IDENTITY_INSERT [CourseChapters] OFF;

            ------------------------------------------------------------
            -- UPSERT Lessons
            ------------------------------------------------------------
            SET IDENTITY_INSERT [Lessons] ON;
            ;WITH Source ([Id], [ChapterId], [Title], [LessonType], [DurationMinutes], [SortOrder], [IsRequired]) AS
            (
                SELECT * FROM (VALUES
                    (1,1,N'REST API',        N'VIDEO',    20,1,1),(2,1, N'JWT Auth',      N'DOCUMENT',30,2,1),
                    (3,2,N'Migration',       N'VIDEO',    25,1,1),(4,2, N'LINQ',          N'VIDEO',   35,2,1),
                    (5,3,N'TF-IDF',          N'DOCUMENT', 40,1,1),(6,3, N'Cosine Sim',   N'VIDEO',   30,2,1),
                    (7,4,N'Dockerfile',      N'VIDEO',    20,1,1),(8,4, N'AWS ECR',       N'DOCUMENT',25,2,1),
                    (9,5,N'k6 Scripting',    N'VIDEO',    45,1,1),(10,5,N'VU config',     N'DOCUMENT',20,2,1),
                    (11,6,N'Viết Use Case',  N'VIDEO',    30,1,1),(12,6,N'Viết FRS',      N'DOCUMENT',40,2,1),
                    (13,7,N'React Hooks',    N'VIDEO',    25,1,1),(14,7,N'Redux',          N'VIDEO',   35,2,1),
                    (15,8,N'DAX Filter',     N'DOCUMENT', 30,1,1),(16,8,N'Data Viz',      N'VIDEO',   20,2,1),
                    (17,9,N'YAML file',      N'VIDEO',    25,1,1),(18,9,N'Runners',        N'DOCUMENT',30,2,1),
                    (19,10,N'Sprint Plan',   N'VIDEO',    20,1,1),(20,10,N'Retro',         N'DOCUMENT',25,2,1)
                ) AS V ([Id], [ChapterId], [Title], [LessonType], [DurationMinutes], [SortOrder], [IsRequired])
            )
            MERGE [Lessons] AS T
            USING Source AS S ON T.[Id] = S.[Id]
            WHEN NOT MATCHED BY TARGET THEN
                INSERT ([Id], [ChapterId], [Title], [LessonType], [DurationMinutes], [SortOrder], [IsRequired])
                VALUES (S.[Id], S.[ChapterId], S.[Title], S.[LessonType], S.[DurationMinutes], S.[SortOrder], S.[IsRequired]);
            SET IDENTITY_INSERT [Lessons] OFF;

            ------------------------------------------------------------
            -- UPSERT Enrollments (unique key: InternUserId + CourseId)
            ------------------------------------------------------------
            ;WITH Source ([InternUserId], [CourseId], [EnrollDate], [CompletionPercent]) AS
            (
                SELECT * FROM (VALUES
                    (N'U_INTERN_001',1,GETDATE(),100),(N'U_INTERN_001',4,GETDATE(),100),(N'U_INTERN_002',2,GETDATE(),50),
                    (N'U_INTERN_003',3,GETDATE(),80), (N'U_INTERN_004',1,GETDATE(),10), (N'U_INTERN_006',3,GETDATE(),95),
                    (N'U_INTERN_007',5,GETDATE(),100),(N'U_INTERN_008',6,GETDATE(),90), (N'U_INTERN_009',1,GETDATE(),50),
                    (N'U_INTERN_010',6,GETDATE(),80), (N'U_INTERN_011',7,GETDATE(),30), (N'U_INTERN_012',8,GETDATE(),60),
                    (N'U_INTERN_013',9,GETDATE(),100),(N'U_INTERN_014',10,GETDATE(),100),(N'U_INTERN_015',10,GETDATE(),0),
                    (N'U_INTERN_016',1,GETDATE(),10), (N'U_INTERN_017',2,GETDATE(),25), (N'U_INTERN_018',3,GETDATE(),10),
                    (N'U_INTERN_019',5,GETDATE(),65), (N'U_INTERN_020',6,GETDATE(),45), (N'U_INTERN_021',7,GETDATE(),85),
                    (N'U_INTERN_022',8,GETDATE(),30), (N'U_INTERN_023',4,GETDATE(),100),(N'U_INTERN_024',9,GETDATE(),40),
                    (N'U_INTERN_025',10,GETDATE(),100),(N'U_INTERN_026',1,GETDATE(),70),(N'U_INTERN_027',1,GETDATE(),0),
                    (N'U_INTERN_028',5,GETDATE(),20), (N'U_INTERN_029',4,GETDATE(),15), (N'U_INTERN_030',6,GETDATE(),100)
                ) AS V ([InternUserId], [CourseId], [EnrollDate], [CompletionPercent])
            )
            MERGE [Enrollments] AS T
            USING Source AS S ON T.[InternUserId] = S.[InternUserId] AND T.[CourseId] = S.[CourseId]
            WHEN MATCHED THEN UPDATE SET T.[CompletionPercent] = S.[CompletionPercent]
            WHEN NOT MATCHED BY TARGET THEN
                INSERT ([InternUserId], [CourseId], [EnrollDate], [CompletionPercent])
                VALUES (S.[InternUserId], S.[CourseId], S.[EnrollDate], S.[CompletionPercent]);

            ------------------------------------------------------------
            -- UPSERT InternshipPeriods
            ------------------------------------------------------------
            SET IDENTITY_INSERT [InternshipPeriods] ON;
            ;WITH Source ([Id], [Name], [StartDate], [EndDate], [IsActive]) AS
            (
                SELECT * FROM (VALUES
                    (1, N'Kỳ thực tập Spring 2026', DATEADD(DAY,-30,GETDATE()), DATEADD(DAY,60,GETDATE()), 1)
                ) AS V ([Id], [Name], [StartDate], [EndDate], [IsActive])
            )
            MERGE [InternshipPeriods] AS T
            USING Source AS S ON T.[Id] = S.[Id]
            WHEN NOT MATCHED BY TARGET THEN
                INSERT ([Id], [Name], [StartDate], [EndDate], [IsActive])
                VALUES (S.[Id], S.[Name], S.[StartDate], S.[EndDate], S.[IsActive]);
            SET IDENTITY_INSERT [InternshipPeriods] OFF;

            ------------------------------------------------------------
            -- UPSERT InternAssignments (unique key: InternUserId + PeriodId)
            ------------------------------------------------------------
            ;WITH InternOrder AS
            (
                SELECT ROW_NUMBER() OVER (ORDER BY u.Id) AS Rn, u.Id AS InternUserId
                FROM [AppUsers] u WHERE u.Id LIKE N'U_INTERN%'
            ),
            Source AS
            (
                SELECT
                    io.InternUserId,
                    CASE io.Rn % 4
                        WHEN 1 THEN N'U_MENTOR_001'
                        WHEN 2 THEN N'U_MENTOR_002'
                        WHEN 3 THEN N'U_MENTOR_003'
                        ELSE        N'U_MENTOR_004'
                    END AS MentorUserId,
                    ip.Id AS PeriodId,
                    GETDATE() AS AssignedDate
                FROM InternOrder io
                CROSS JOIN [InternshipPeriods] ip
                WHERE ip.[Name] = N'Kỳ thực tập Spring 2026'
            )
            MERGE [InternAssignments] AS T
            USING Source AS S ON T.[InternUserId] = S.[InternUserId] AND T.[PeriodId] = S.[PeriodId]
            WHEN MATCHED THEN UPDATE SET T.[MentorUserId] = S.[MentorUserId]
            WHEN NOT MATCHED BY TARGET THEN
                INSERT ([InternUserId], [MentorUserId], [PeriodId], [AssignedDate])
                VALUES (S.[InternUserId], S.[MentorUserId], S.[PeriodId], S.[AssignedDate]);

            ------------------------------------------------------------
            -- UPSERT TaskItems (unique key: AssignmentId + Title)
            ------------------------------------------------------------
            ;WITH TaskSource ([Rn], [Title], [Description], [Priority], [Status], [Deadline], [EstimatedHours], [CreateDate], [CreatedByUserId]) AS
            (
                SELECT * FROM (VALUES
                    (1, N'API JWT',                   N'Login endpoint',      N'HIGH',   N'DONE',        DATEADD(DAY,2,GETDATE()),  10, GETDATE(), N'U_MENTOR_001'),
                    (1, N'Docker AWS',                N'Push image ECR',      N'HIGH',   N'DONE',        DATEADD(DAY,3,GETDATE()),  12, GETDATE(), N'U_MENTOR_001'),
                    (2, N'TF-IDF CV',                 N'Extract skills',      N'HIGH',   N'IN_PROGRESS', DATEADD(DAY,5,GETDATE()),  16, GETDATE(), N'U_MENTOR_002'),
                    (3, N'k6 Load Test',              N'100 VUs login',       N'MEDIUM', N'DONE',        DATEADD(DAY,1,GETDATE()),   8, GETDATE(), N'U_MENTOR_003'),
                    (4, N'Use Case Quản lý thực đơn', N'Vẽ bằng Draw.io',    N'HIGH',   N'DONE',        DATEADD(DAY,4,GETDATE()),  10, GETDATE(), N'U_MENTOR_004'),
                    (5, N'UI 44px touch',             N'Fix mobile UI',       N'MEDIUM', N'TODO',        DATEADD(DAY,6,GETDATE()),   6, GETDATE(), N'U_MENTOR_001'),
                    (6, N'Migration DB',              N'LMS module',          N'HIGH',   N'IN_PROGRESS', DATEADD(DAY,2,GETDATE()),  14, GETDATE(), N'U_MENTOR_001'),
                    (7, N'Cosine Sim',                N'Matching AI',         N'HIGH',   N'TODO',        DATEADD(DAY,8,GETDATE()),  20, GETDATE(), N'U_MENTOR_002'),
                    (8, N'Test API',                  N'Postman runner',      N'LOW',    N'DONE',        DATEADD(DAY,1,GETDATE()),   4, GETDATE(), N'U_MENTOR_003'),
                    (9, N'FRS Document',              N'Phần Đăng nhập',      N'HIGH',   N'IN_PROGRESS', DATEADD(DAY,3,GETDATE()),  16, GETDATE(), N'U_MENTOR_004'),
                    (10,N'Fix Bug #101',              N'Fix crash login',     N'HIGH',   N'DONE',        DATEADD(DAY,2,GETDATE()),   4, GETDATE(), N'U_MENTOR_001'),
                    (11,N'Data Crawl',                N'Crawl JDs',           N'MEDIUM', N'IN_PROGRESS', DATEADD(DAY,4,GETDATE()),  10, GETDATE(), N'U_MENTOR_002'),
                    (12,N'Automation UI',             N'Selenium script',     N'HIGH',   N'TODO',        DATEADD(DAY,6,GETDATE()),  12, GETDATE(), N'U_MENTOR_003'),
                    (13,N'BPMN Quy trình',            N'Tuyển dụng flow',     N'MEDIUM', N'DONE',        DATEADD(DAY,1,GETDATE()),   8, GETDATE(), N'U_MENTOR_004'),
                    (14,N'Redis Cache',               N'Cache API',           N'HIGH',   N'TODO',        DATEADD(DAY,5,GETDATE()),  12, GETDATE(), N'U_MENTOR_001'),
                    (15,N'Log ELK',                   N'Config Kibana',       N'MEDIUM', N'IN_PROGRESS', DATEADD(DAY,7,GETDATE()),  16, GETDATE(), N'U_MENTOR_001'),
                    (16,N'Model Train',               N'Train ML.NET',        N'HIGH',   N'TODO',        DATEADD(DAY,10,GETDATE()), 24, GETDATE(), N'U_MENTOR_002'),
                    (17,N'Jmeter Test',               N'Load test LMS',       N'MEDIUM', N'DONE',        DATEADD(DAY,-1,GETDATE()),  8, GETDATE(), N'U_MENTOR_003'),
                    (18,N'UML Class',                 N'Class Diagram',       N'LOW',    N'IN_PROGRESS', DATEADD(DAY,2,GETDATE()),   6, GETDATE(), N'U_MENTOR_004'),
                    (19,N'React Router',              N'Setup routes',        N'HIGH',   N'DONE',        DATEADD(DAY,1,GETDATE()),   4, GETDATE(), N'U_MENTOR_001'),
                    (20,N'Clean Data',                N'Remove stopwords',    N'MEDIUM', N'TODO',        DATEADD(DAY,3,GETDATE()),   8, GETDATE(), N'U_MENTOR_002'),
                    (21,N'Bug report',                N'Log Jira',            N'LOW',    N'DONE',        DATEADD(DAY,0,GETDATE()),   2, GETDATE(), N'U_MENTOR_003'),
                    (22,N'Wireframe',                 N'Dashboard UI',        N'HIGH',   N'IN_PROGRESS', DATEADD(DAY,5,GETDATE()),  12, GETDATE(), N'U_MENTOR_004'),
                    (23,N'CI/CD Pipeline',            N'Github Actions',      N'HIGH',   N'TODO',        DATEADD(DAY,7,GETDATE()),  16, GETDATE(), N'U_MENTOR_001'),
                    (24,N'Chatbot AI',                N'Dialogflow',          N'MEDIUM', N'IN_PROGRESS', DATEADD(DAY,9,GETDATE()),  20, GETDATE(), N'U_MENTOR_002'),
                    (25,N'Pen Test',                  N'OWASP Top 10',        N'HIGH',   N'TODO',        DATEADD(DAY,12,GETDATE()), 24, GETDATE(), N'U_MENTOR_003'),
                    (26,N'User Story',                N'Sprint 1',            N'MEDIUM', N'DONE',        DATEADD(DAY,-2,GETDATE()),  8, GETDATE(), N'U_MENTOR_004'),
                    (27,N'Entity Rel',                N'ERD Diagram',         N'HIGH',   N'IN_PROGRESS', DATEADD(DAY,1,GETDATE()),   6, GETDATE(), N'U_MENTOR_001'),
                    (28,N'AWS S3',                    N'Upload CV API',       N'MEDIUM', N'TODO',        DATEADD(DAY,4,GETDATE()),  10, GETDATE(), N'U_MENTOR_001'),
                    (29,N'Review Doc',                N'Peer review',         N'LOW',    N'DONE',        DATEADD(DAY,-1,GETDATE()),  4, GETDATE(), N'U_MENTOR_004')
                ) AS V ([Rn], [Title], [Description], [Priority], [Status], [Deadline], [EstimatedHours], [CreateDate], [CreatedByUserId])
            ),
            InternOrder AS
            (
                SELECT ROW_NUMBER() OVER (ORDER BY u.Id) AS Rn, u.Id AS InternUserId
                FROM [AppUsers] u WHERE u.Id LIKE N'U_INTERN%'
            ),
            Source AS
            (
                SELECT ia.Id AS AssignmentId,
                       ts.Title, ts.Description, ts.Priority, ts.Status,
                       ts.Deadline, ts.EstimatedHours, ts.CreateDate, ts.CreatedByUserId
                FROM TaskSource ts
                INNER JOIN InternOrder io ON io.Rn = ts.Rn
                INNER JOIN [InternshipPeriods] ip ON ip.[Name] = N'Kỳ thực tập Spring 2026'
                INNER JOIN [InternAssignments] ia ON ia.InternUserId = io.InternUserId AND ia.PeriodId = ip.Id
            )
            MERGE [TaskItems] AS T
            USING Source AS S ON T.[AssignmentId] = S.[AssignmentId] AND T.[Title] = S.[Title]
            WHEN MATCHED THEN
                UPDATE SET T.[Description] = S.[Description], T.[Priority] = S.[Priority],
                           T.[Status] = S.[Status], T.[Deadline] = S.[Deadline],
                           T.[EstimatedHours] = S.[EstimatedHours], T.[CreatedByUserId] = S.[CreatedByUserId]
            WHEN NOT MATCHED BY TARGET THEN
                INSERT ([Title], [Description], [AssignmentId], [Priority], [Status], [Deadline], [EstimatedHours], [CreateDate], [CreatedByUserId])
                VALUES (S.[Title], S.[Description], S.[AssignmentId], S.[Priority], S.[Status], S.[Deadline], S.[EstimatedHours], S.[CreateDate], S.[CreatedByUserId]);

            ------------------------------------------------------------
            -- UPSERT DailyReports (unique key: InternUserId + date(ReportDate))
            ------------------------------------------------------------
            DELETE dr
            FROM [DailyReports] dr
            WHERE CONVERT(date, dr.[ReportDate]) = CONVERT(date, @SeedDate)
              AND dr.[InternUserId] LIKE N'U_INTERN_%'
              AND dr.[ReviewedByMentorId] LIKE N'U_MENTOR_%'
              AND dr.[Content] IN (
                    N'Đẩy xong Docker image lên ECR',
                    N'Migration DB bị lỗi FK',
                    N'Bóc tách được 50 từ khóa CV',
                    N'Làm mockup Figma',
                    N'Học OWASP',
                    N'Viết User Story',
                    N'Chạy script k6 pass 100 VUs',
                    N'Vẽ xong Use Case Quản lý thực đơn',
                    N'Test API login bằng Postman',
                    N'Vẽ ERD',
                    N'Code React component',
                    N'Crawl data JD',
                    N'Viết script Selenium',
                    N'Vẽ BPMN',
                    N'Đọc tài liệu Agile',
                    N'Setup S3 bucket',
                    N'Đọc tài liệu',
                    N'Tìm hiểu Redis',
                    N'Setup ELK',
                    N'Tìm hiểu ML.NET',
                    N'Chạy Jmeter',
                    N'Vẽ Class Diagram',
                    N'Setup xong Github Actions',
                    N'Code React Router',
                    N'Clear stopwords',
                    N'Log bug lên Jira',
                    N'Làm Wireframe',
                    N'Build pipeline',
                    N'Nghiên cứu Dialogflow',
                    N'Peer review FRS của team'
                );

            ;WITH Source ([InternUserId], [ReportDate], [Content], [PlannedTomorrow], [MentorFeedback], [ReviewedByMentorId]) AS
            (
                SELECT * FROM (VALUES
                    (N'U_INTERN_001',@DemoReportDate,N'Đẩy xong Docker image lên ECR',       N'Tích hợp ECS',     N'Tốt, nhớ check IAM policy',       N'U_MENTOR_001'),
                    (N'U_INTERN_002',@DemoReportDate,N'Migration DB bị lỗi FK',              N'Fix lỗi cascade',  N'Xem lại ERD',                     N'U_MENTOR_001'),
                    (N'U_INTERN_003',@DemoReportDate,N'Bóc tách được 50 từ khóa CV',         N'Tính TF-IDF',      N'Cần lọc thêm stopwords',          N'U_MENTOR_002'),
                    (N'U_INTERN_004',@DemoReportDate,N'Làm mockup Figma',                    N'Xin review',       N'Cần chú ý 44px touch target',     N'U_MENTOR_004'),
                    (N'U_INTERN_005',@DemoReportDate,N'Học OWASP',                           N'Test SQLi',        N'Ok',                              N'U_MENTOR_003'),
                    (N'U_INTERN_006',@DemoReportDate,N'Viết User Story',                     N'Estimation',       N'Ok',                              N'U_MENTOR_004'),
                    (N'U_INTERN_007',@DemoReportDate,N'Chạy script k6 pass 100 VUs',         N'Report kết quả',   N'Kiểm tra lại RAM usage',          N'U_MENTOR_003'),
                    (N'U_INTERN_008',@DemoReportDate,N'Vẽ xong Use Case Quản lý thực đơn',  N'Viết FRS',         N'Logic đúng chuẩn',                N'U_MENTOR_004'),
                    (N'U_INTERN_009',@DemoReportDate,N'Test API login bằng Postman',         N'Test API Register',N'Ok',                              N'U_MENTOR_003'),
                    (N'U_INTERN_010',@DemoReportDate,N'Vẽ ERD',                              N'Migration',        N'Ok',                              N'U_MENTOR_001'),
                    (N'U_INTERN_011',@DemoReportDate,N'Code React component',                N'Ghép API',         N'Ok',                              N'U_MENTOR_001'),
                    (N'U_INTERN_012',@DemoReportDate,N'Crawl data JD',                       N'Clean data',       N'Ok',                              N'U_MENTOR_002'),
                    (N'U_INTERN_013',@DemoReportDate,N'Viết script Selenium',                N'Chạy thử',         N'Ok',                              N'U_MENTOR_003'),
                    (N'U_INTERN_014',@DemoReportDate,N'Vẽ BPMN',                             N'Review',           N'Ok',                              N'U_MENTOR_004'),
                    (N'U_INTERN_015',@DemoReportDate,N'Đọc tài liệu Agile',                 N'Học Jira',         N'Tốt',                             N'U_MENTOR_004'),
                    (N'U_INTERN_016',@DemoReportDate,N'Setup S3 bucket',                     N'Code API',         N'Ok',                              N'U_MENTOR_001'),
                    (N'U_INTERN_017',@DemoReportDate,N'Đọc tài liệu',                        N'Peer review',      N'Ok',                              N'U_MENTOR_004'),
                    (N'U_INTERN_018',@DemoReportDate,N'Tìm hiểu Redis',                      N'Code demo',        N'Ok',                              N'U_MENTOR_001'),
                    (N'U_INTERN_019',@DemoReportDate,N'Setup ELK',                           N'Config Logstash',  N'Ok',                              N'U_MENTOR_001'),
                    (N'U_INTERN_020',@DemoReportDate,N'Tìm hiểu ML.NET',                    N'Train model',      N'Ok',                              N'U_MENTOR_002'),
                    (N'U_INTERN_021',@DemoReportDate,N'Chạy Jmeter',                         N'Đọc report',       N'Ok',                              N'U_MENTOR_003'),
                    (N'U_INTERN_022',@DemoReportDate,N'Vẽ Class Diagram',                    N'Code entity',      N'Ok',                              N'U_MENTOR_004'),
                    (N'U_INTERN_023',@DemoReportDate,N'Setup xong Github Actions',           N'Test push code',   N'Tốt',                             N'U_MENTOR_001'),
                    (N'U_INTERN_024',@DemoReportDate,N'Code React Router',                   N'Code UI',          N'Ok',                              N'U_MENTOR_001'),
                    (N'U_INTERN_025',@DemoReportDate,N'Clear stopwords',                     N'NLP',              N'Ok',                              N'U_MENTOR_002'),
                    (N'U_INTERN_026',@DemoReportDate,N'Log bug lên Jira',                    N'Verify bug',       N'Ok',                              N'U_MENTOR_003'),
                    (N'U_INTERN_027',@DemoReportDate,N'Làm Wireframe',                       N'UI design',        N'Ok',                              N'U_MENTOR_004'),
                    (N'U_INTERN_028',@DemoReportDate,N'Build pipeline',                      N'Deploy test',      N'Ok',                              N'U_MENTOR_001'),
                    (N'U_INTERN_029',@DemoReportDate,N'Nghiên cứu Dialogflow',               N'Tạo bot',          N'Ok',                              N'U_MENTOR_002'),
                    (N'U_INTERN_030',@DemoReportDate,N'Peer review FRS của team',            N'Hoàn thiện docs',  N'Review kỹ phần rule',             N'U_MENTOR_004')
                ) AS V ([InternUserId], [ReportDate], [Content], [PlannedTomorrow], [MentorFeedback], [ReviewedByMentorId])
            )
            MERGE [DailyReports] AS T
            USING Source AS S ON T.[InternUserId] = S.[InternUserId]
                              AND CONVERT(date, T.[ReportDate]) = CONVERT(date, S.[ReportDate])
            WHEN MATCHED THEN
                UPDATE SET T.[Content] = S.[Content], T.[PlannedTomorrow] = S.[PlannedTomorrow],
                           T.[MentorFeedback] = S.[MentorFeedback], T.[ReviewedByMentorId] = S.[ReviewedByMentorId]
            WHEN NOT MATCHED BY TARGET THEN
                INSERT ([InternUserId], [ReportDate], [Content], [PlannedTomorrow], [MentorFeedback], [ReviewedByMentorId])
                VALUES (S.[InternUserId], S.[ReportDate], S.[Content], S.[PlannedTomorrow], S.[MentorFeedback], S.[ReviewedByMentorId]);

            COMMIT;
            PRINT N'✅ AIMS seed data upserted successfully.';
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK;
            DECLARE @Msg nvarchar(4000) = ERROR_MESSAGE();
            DECLARE @Sev int           = ERROR_SEVERITY();
            DECLARE @St  int           = ERROR_STATE();
            PRINT @Msg;
            RAISERROR(@Msg, @Sev, @St);
        END CATCH;
        """;
}
