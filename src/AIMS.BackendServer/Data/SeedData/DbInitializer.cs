using AIMS.BackendServer.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AIMS.BackendServer.Data.SeedData;

/// <summary>
/// Khởi tạo dữ liệu mẫu cho hệ thống AIMS — PostgreSQL edition.
///
/// Chiến lược 2 bước:
///   1. C# (Identity)  → Tạo roles + users thực với password hash đúng.
///   2. SQL (PL/pgSQL) → UPSERT toàn bộ dữ liệu bulk qua DO $$ ... $$ block.
///                        Chạy lại nhiều lần không bị lỗi duplicate.
///
/// Khác biệt so với SQL Server:
///   - MERGE           → INSERT ... ON CONFLICT ... DO UPDATE / DO NOTHING
///   - SET IDENTITY_INSERT ON/OFF → không cần, PostgreSQL cho phép insert explicit Id
///   - Sau mỗi explicit-Id insert → PERFORM setval() để reset sequence
///   - GETDATE()       → NOW()
///   - DATEADD()       → interval arithmetic
///   - NEWID()         → gen_random_uuid()::TEXT
///   - N'string'       → 'string'  (không cần prefix N)
///   - @var            → v_var trong PL/pgSQL
///   - RAISERROR       → RAISE EXCEPTION
///   - PRINT           → RAISE NOTICE
///   - + (concat)      → ||
///   - CAST(x AS NVARCHAR) → x::TEXT
///   - CONVERT(date,x) → x::DATE
///   - 1/0 (boolean)   → TRUE/FALSE
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
        await context.Database.ExecuteSqlRawAsync(SeedUniversitiesSql); // ← seed Universities trước
        await SeedUsersAsync(userManager);                               // ← FK thỏa mãn
        await context.Database.ExecuteSqlRawAsync(FullScaleSeedSql);
    }

    // ═══════════════════════════════════════════════════════════
    // Bước 1a — Seed roles qua Identity
    // ═══════════════════════════════════════════════════════════
    private const string SeedUniversitiesSql = """
    INSERT INTO "Universities" ("Id", "Name", "City") VALUES
        (1,  'Đại học Bách khoa Hà Nội',      'Hà Nội'),
        (2,  'Đại học Công nghệ - ĐHQGHN',    'Hà Nội'),
        (3,  'Học viện Công nghệ BCVT',        'Hà Nội'),
        (4,  'Đại học FPT',                    'Hà Nội'),
        (5,  'Đại học KHTN - ĐHQG TP.HCM',    'TP.HCM'),
        (6,  'Đại học Duy Tân',                'Đà Nẵng'),
        (7,  'Đại học Thương Mại (TMU)',       'Hà Nội'),
        (8,  'Đại học Kinh tế Quốc dân',      'Hà Nội'),
        (9,  'Đại học Giao thông Vận tải',    'Hà Nội'),
        (10, 'Học viện Ngân hàng',             'Hà Nội'),
        (11, 'Học viện Kỹ thuật Mật mã',      'Hà Nội'),
        (12, 'Đại học Công nghiệp Hà Nội',    'Hà Nội'),
        (13, 'Đại học Sư phạm Kỹ thuật',      'TP.HCM'),
        (14, 'Đại học Bách khoa TP.HCM',      'TP.HCM'),
        (15, 'Đại học Tôn Đức Thắng',         'TP.HCM')
    ON CONFLICT ("Id") DO NOTHING;

    SELECT setval(
        pg_get_serial_sequence('"Universities"', 'Id'),
        (SELECT MAX("Id") FROM "Universities")
    );
    """;
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
    // Bước 1b — Seed users thực với password hash qua Identity
    // ═══════════════════════════════════════════════════════════

    private static async Task SeedUsersAsync(UserManager<AppUser> userManager)
    {
        const string AdminPassword = "Admin@2025!";
        const string HrPassword = "Hr@2025!";
        const string MentorPassword = "Mentor@2025!";
        const string InternPassword = "Intern@2025!";

        var users = new[]
        {
            (Id:"U_ADMIN_001",  Email:"admin@deha.vn",    UserName:"admin",          Pass:AdminPassword,  Role:"Admin",  First:"Admin",  Last:"System",         StudentId:(string?)null, GPA:(decimal?)null, UniversityId:(int?)null),
            (Id:"U_HR_001",     Email:"hr.minh@deha.vn",  UserName:"hr.minh",        Pass:HrPassword,     Role:"HR",     First:"Minh",   Last:"Nguyễn HR",      StudentId:(string?)null, GPA:(decimal?)null, UniversityId:(int?)null),
            (Id:"U_HR_002",     Email:"hr.lan@deha.vn",   UserName:"hr.lan",         Pass:HrPassword,     Role:"HR",     First:"Lan",    Last:"Phạm HR",        StudentId:(string?)null, GPA:(decimal?)null, UniversityId:(int?)null),
            (Id:"U_MENTOR_001", Email:"hoang@deha.vn",    UserName:"mentor.hoang",   Pass:MentorPassword, Role:"Mentor", First:"Hoàng",  Last:"Backend",        StudentId:(string?)null, GPA:(decimal?)null, UniversityId:(int?)null),
            (Id:"U_MENTOR_002", Email:"anh@deha.vn",      UserName:"mentor.anh",     Pass:MentorPassword, Role:"Mentor", First:"Anh",    Last:"AI_NLP",         StudentId:(string?)null, GPA:(decimal?)null, UniversityId:(int?)null),
            (Id:"U_MENTOR_003", Email:"duc@deha.vn",      UserName:"mentor.duc",     Pass:MentorPassword, Role:"Mentor", First:"Đức",    Last:"QA_QC",          StudentId:(string?)null, GPA:(decimal?)null, UniversityId:(int?)null),
            (Id:"U_MENTOR_004", Email:"huong@deha.vn",    UserName:"mentor.huong",   Pass:MentorPassword, Role:"Mentor", First:"Hương",  Last:"BA_Lead",        StudentId:(string?)null, GPA:(decimal?)null, UniversityId:(int?)null),
            (Id:"U_INTERN_001", Email:"thanh@sv.vn",      UserName:"intern.thanh",   Pass:InternPassword, Role:"Intern", First:"Thanh",  Last:"Trần Phương",    StudentId:"SV001", GPA:3.85m, UniversityId:7),
            (Id:"U_INTERN_002", Email:"nam@sv.vn",        UserName:"intern.nam",     Pass:InternPassword, Role:"Intern", First:"Nam",    Last:"Nguyễn Hoài",    StudentId:"SV002", GPA:3.20m, UniversityId:2),
            (Id:"U_INTERN_003", Email:"linh@sv.vn",       UserName:"intern.linh",    Pass:InternPassword, Role:"Intern", First:"Linh",   Last:"Vũ Khánh",       StudentId:"SV003", GPA:3.72m, UniversityId:3),
            (Id:"U_INTERN_004", Email:"dung@sv.vn",       UserName:"intern.dung",    Pass:InternPassword, Role:"Intern", First:"Dũng",   Last:"Phạm Minh",      StudentId:"SV004", GPA:2.95m, UniversityId:4),
            (Id:"U_INTERN_005", Email:"ha@sv.vn",         UserName:"intern.ha",      Pass:InternPassword, Role:"Intern", First:"Hà",     Last:"Đỗ Ngọc",        StudentId:"SV005", GPA:3.10m, UniversityId:5),
            (Id:"U_INTERN_006", Email:"quan@sv.vn",       UserName:"intern.quan",    Pass:InternPassword, Role:"Intern", First:"Quân",   Last:"Bùi Anh",        StudentId:"SV006", GPA:3.60m, UniversityId:6),
            (Id:"U_INTERN_007", Email:"hai@sv.vn",        UserName:"intern.hai",     Pass:InternPassword, Role:"Intern", First:"Hải",    Last:"Trần Ngọc",      StudentId:"SV007", GPA:3.50m, UniversityId:7),
            (Id:"U_INTERN_008", Email:"thao@sv.vn",       UserName:"intern.thao",    Pass:InternPassword, Role:"Intern", First:"Thảo",   Last:"Lê Phương",      StudentId:"SV008", GPA:3.65m, UniversityId:8),
            (Id:"U_INTERN_009", Email:"phong@sv.vn",      UserName:"intern.phong",   Pass:InternPassword, Role:"Intern", First:"Phong",  Last:"Nguyễn Đình",    StudentId:"SV009", GPA:3.15m, UniversityId:1),
            (Id:"U_INTERN_010", Email:"yen@sv.vn",        UserName:"intern.yen",     Pass:InternPassword, Role:"Intern", First:"Yến",    Last:"Hoàng Hải",      StudentId:"SV010", GPA:3.80m, UniversityId:7),
            (Id:"U_INTERN_011", Email:"cuong@sv.vn",      UserName:"intern.cuong",   Pass:InternPassword, Role:"Intern", First:"Cường",  Last:"Vũ Quốc",        StudentId:"SV011", GPA:3.00m, UniversityId:9),
            (Id:"U_INTERN_012", Email:"mai@sv.vn",        UserName:"intern.mai",     Pass:InternPassword, Role:"Intern", First:"Mai",    Last:"Đặng Phương",    StudentId:"SV012", GPA:3.40m, UniversityId:10),
            (Id:"U_INTERN_013", Email:"tuan@sv.vn",       UserName:"intern.tuan",    Pass:InternPassword, Role:"Intern", First:"Tuấn",   Last:"Đinh Khắc",      StudentId:"SV013", GPA:2.80m, UniversityId:11),
            (Id:"U_INTERN_014", Email:"trang@sv.vn",      UserName:"intern.trang",   Pass:InternPassword, Role:"Intern", First:"Trang",  Last:"Lý Thu",         StudentId:"SV014", GPA:3.55m, UniversityId:12),
            (Id:"U_INTERN_015", Email:"khoa@sv.vn",       UserName:"intern.khoa",    Pass:InternPassword, Role:"Intern", First:"Khoa",   Last:"Hồ Đăng",        StudentId:"SV015", GPA:3.70m, UniversityId:13),
            (Id:"U_INTERN_016", Email:"binh@sv.vn",       UserName:"intern.binh",    Pass:InternPassword, Role:"Intern", First:"Bình",   Last:"Trương Thanh",   StudentId:"SV016", GPA:3.10m, UniversityId:14),
            (Id:"U_INTERN_017", Email:"hoa@sv.vn",        UserName:"intern.hoa",     Pass:InternPassword, Role:"Intern", First:"Hoa",    Last:"Ngô Quý",        StudentId:"SV017", GPA:3.25m, UniversityId:15),
            (Id:"U_INTERN_018", Email:"viet@sv.vn",       UserName:"intern.viet",    Pass:InternPassword, Role:"Intern", First:"Việt",   Last:"Bùi Quang",      StudentId:"SV018", GPA:2.90m, UniversityId:1),
            (Id:"U_INTERN_019", Email:"giang@sv.vn",      UserName:"intern.giang",   Pass:InternPassword, Role:"Intern", First:"Giang",  Last:"Phạm Hương",     StudentId:"SV019", GPA:3.65m, UniversityId:2),
            (Id:"U_INTERN_020", Email:"dat@sv.vn",        UserName:"intern.dat",     Pass:InternPassword, Role:"Intern", First:"Đạt",    Last:"Nguyễn Thành",   StudentId:"SV020", GPA:3.80m, UniversityId:3),
            (Id:"U_INTERN_021", Email:"my@sv.vn",         UserName:"intern.my",      Pass:InternPassword, Role:"Intern", First:"My",     Last:"Trần Trà",       StudentId:"SV021", GPA:3.40m, UniversityId:4),
            (Id:"U_INTERN_022", Email:"long@sv.vn",       UserName:"intern.long",    Pass:InternPassword, Role:"Intern", First:"Long",   Last:"Hoàng Phi",      StudentId:"SV022", GPA:2.85m, UniversityId:5),
            (Id:"U_INTERN_023", Email:"han@sv.vn",        UserName:"intern.han",     Pass:InternPassword, Role:"Intern", First:"Hân",    Last:"Đỗ Gia",         StudentId:"SV023", GPA:3.90m, UniversityId:6),
            (Id:"U_INTERN_024", Email:"son@sv.vn",        UserName:"intern.son",     Pass:InternPassword, Role:"Intern", First:"Sơn",    Last:"Lê Tùng",        StudentId:"SV024", GPA:3.05m, UniversityId:7),
            (Id:"U_INTERN_025", Email:"an@sv.vn",         UserName:"intern.an",      Pass:InternPassword, Role:"Intern", First:"An",     Last:"Vũ Bình",        StudentId:"SV025", GPA:3.35m, UniversityId:8),
            (Id:"U_INTERN_026", Email:"phuc@sv.vn",       UserName:"intern.phuc",    Pass:InternPassword, Role:"Intern", First:"Phúc",   Last:"Nguyễn Hồng",    StudentId:"SV026", GPA:3.15m, UniversityId:9),
            (Id:"U_INTERN_027", Email:"tam@sv.vn",        UserName:"intern.tam",     Pass:InternPassword, Role:"Intern", First:"Tâm",    Last:"Trần Minh",      StudentId:"SV027", GPA:3.45m, UniversityId:10),
            (Id:"U_INTERN_028", Email:"khanh@sv.vn",      UserName:"intern.khanh",   Pass:InternPassword, Role:"Intern", First:"Khánh",  Last:"Lý Quốc",        StudentId:"SV028", GPA:2.75m, UniversityId:11),
            (Id:"U_INTERN_029", Email:"nhi@sv.vn",        UserName:"intern.nhi",     Pass:InternPassword, Role:"Intern", First:"Nhi",    Last:"Đặng Yến",       StudentId:"SV029", GPA:3.60m, UniversityId:12),
            (Id:"U_INTERN_030", Email:"bao@sv.vn",        UserName:"intern.bao",     Pass:InternPassword, Role:"Intern", First:"Bảo",    Last:"Phạm Gia",       StudentId:"SV030", GPA:3.85m, UniversityId:13),
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
                    UniversityId = u.UniversityId,
                    CreateDate = DateTime.UtcNow,
                };

                var createResult = await userManager.CreateAsync(user, u.Pass);
                if (!createResult.Succeeded)
                    throw new InvalidOperationException(
                        $"Cannot create user '{u.Email}': {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }
            else
            {
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
                user.UniversityId = u.UniversityId;
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
    // Bước 2 — Bulk seed toàn bộ dữ liệu qua PL/pgSQL DO block
    // ═══════════════════════════════════════════════════════════

    private const string FullScaleSeedSql = """
        DO $$
        DECLARE
            v_now         TIMESTAMPTZ := NOW();
            v_report_date DATE        := (CURRENT_DATE - INTERVAL '1 day')::DATE;
        BEGIN

        -- ╔══════════════════════════════════════════════════════════════╗
        -- ║  AIMS FULL SCALE SEED DATA — PostgreSQL Safe UPSERT         ║
        -- ║  Chạy lại nhiều lần không lỗi duplicate key                 ║
        -- ╚══════════════════════════════════════════════════════════════╝

        ------------------------------------------------------------
        -- AppRoles
        -- ON CONFLICT target: NormalizedName (unique index từ Identity)
        ------------------------------------------------------------
        INSERT INTO "AppRoles" ("Id", "Name", "NormalizedName", "Description", "ConcurrencyStamp")
        VALUES
            ('ROLE_ADMIN',  'Admin',  'ADMIN',  'Quản trị viên hệ thống, toàn quyền', gen_random_uuid()::TEXT),
            ('ROLE_HR',     'HR',     'HR',     'Nhân viên tuyển dụng',                gen_random_uuid()::TEXT),
            ('ROLE_MENTOR', 'Mentor', 'MENTOR', 'Người hướng dẫn thực tập sinh',       gen_random_uuid()::TEXT),
            ('ROLE_INTERN', 'Intern', 'INTERN', 'Thực tập sinh',                        gen_random_uuid()::TEXT)
        ON CONFLICT ("NormalizedName") DO UPDATE SET
            "Description" = EXCLUDED."Description",
            "Name"        = EXCLUDED."Name";

        ------------------------------------------------------------
        -- Commands
        ------------------------------------------------------------
        INSERT INTO "Commands" ("Id", "Name") VALUES
            ('VIEW',      'Xem'),
            ('CREATE',    'Tạo'),
            ('UPDATE',    'Sửa'),
            ('DELETE',    'Xóa'),
            ('APPROVE',   'Duyệt'),
            ('IMPORT_CV', 'Import CV'),
            ('AI_SCREEN', 'Sàng lọc AI'),
            ('EXPORT',    'Xuất DB')
        ON CONFLICT ("Id") DO NOTHING;

        ------------------------------------------------------------
        -- Functions
        ------------------------------------------------------------
        INSERT INTO "Functions" ("Id", "Name", "Url", "Icon", "SortOrder", "ParentId") VALUES
            ('DASHBOARD',       'Tổng quan',    '/dashboard',                    'fa fa-chart-line',     1, NULL),
            ('RECRUITMENT',     'Tuyển dụng',   '/recruitment',                  'fa fa-user-check',     2, NULL),
            ('LMS',             'Đào tạo',      '/lms',                          'fa fa-graduation-cap', 3, NULL),
            ('TASK_MANAGEMENT', 'Thực tập',     '/tasks',                        'fa fa-tasks',          4, NULL),
            ('REPORT',          'Báo cáo',      '/reports',                      'fa fa-file-alt',       5, NULL),
            ('CV_SCREENING',    'Sàng lọc AI',  '/recruitment/cv-screening',     'fa fa-robot',          1, 'RECRUITMENT'),
            ('JOB_DESCRIPTION', 'Mô tả JD',     '/recruitment/job-descriptions', 'fa fa-briefcase',      2, 'RECRUITMENT'),
            ('COURSE',          'Khóa học',     '/lms/courses',                  'fa fa-book',           1, 'LMS'),
            ('INTERN_TASK',     'Nhiệm vụ',     '/tasks/intern-tasks',           'fa fa-list-check',     1, 'TASK_MANAGEMENT'),
            ('DAILY_REPORT',    'Báo cáo ngày', '/tasks/daily-reports',          'fa fa-pen',            2, 'TASK_MANAGEMENT'),
            ('TIMESHEET',       'Chấm công',    '/tasks/timesheets',             'fa fa-clock',          3, 'TASK_MANAGEMENT')
        ON CONFLICT ("Id") DO NOTHING;

        ------------------------------------------------------------
        -- CommandInFunctions  (composite PK: CommandId + FunctionId)
        ------------------------------------------------------------
        INSERT INTO "CommandInFunctions" ("CommandId", "FunctionId")
        SELECT c."Id", f."Id"
        FROM "Commands" c
        CROSS JOIN "Functions" f
        WHERE c."Id" IN ('VIEW','CREATE','UPDATE','DELETE','EXPORT')
        ON CONFLICT DO NOTHING;

        ------------------------------------------------------------
        -- Permissions cho Admin  (composite PK: FunctionId+RoleId+CommandId)
        ------------------------------------------------------------
        INSERT INTO "Permissions" ("FunctionId", "RoleId", "CommandId")
        SELECT cif."FunctionId", r."Id", cif."CommandId"
        FROM "CommandInFunctions" cif
        CROSS JOIN "AppRoles" r
        WHERE r."NormalizedName" = 'ADMIN'
        ON CONFLICT DO NOTHING;

        ------------------------------------------------------------
        -- Permissions cho Mentor / HR / Intern
        ------------------------------------------------------------
        INSERT INTO "Permissions" ("FunctionId", "RoleId", "CommandId")
        SELECT src."FunctionId", r."Id", src."CommandId"
        FROM (VALUES
            ('MENTOR', 'INTERN_TASK',     'VIEW'),
            ('MENTOR', 'INTERN_TASK',     'CREATE'),
            ('MENTOR', 'INTERN_TASK',     'UPDATE'),
            ('MENTOR', 'INTERN_TASK',     'DELETE'),
            ('MENTOR', 'DAILY_REPORT',    'VIEW'),
            ('MENTOR', 'DAILY_REPORT',    'UPDATE'),
            ('MENTOR', 'JOB_DESCRIPTION', 'VIEW'),
            ('MENTOR', 'JOB_DESCRIPTION', 'CREATE'),
            ('MENTOR', 'JOB_DESCRIPTION', 'UPDATE'),
            ('MENTOR', 'CV_SCREENING',    'VIEW'),
            ('MENTOR', 'CV_SCREENING',    'CREATE'),
            ('HR',     'JOB_DESCRIPTION', 'VIEW'),
            ('HR',     'JOB_DESCRIPTION', 'CREATE'),
            ('HR',     'JOB_DESCRIPTION', 'UPDATE'),
            ('HR',     'JOB_DESCRIPTION', 'DELETE'),
            ('HR',     'CV_SCREENING',    'VIEW'),
            ('HR',     'CV_SCREENING',    'CREATE'),
            ('HR',     'CV_SCREENING',    'UPDATE'),
            ('HR',     'CV_SCREENING',    'DELETE'),
            ('INTERN', 'INTERN_TASK',     'VIEW'),
            ('INTERN', 'INTERN_TASK',     'UPDATE'),
            ('INTERN', 'DAILY_REPORT',    'VIEW'),
            ('INTERN', 'DAILY_REPORT',    'CREATE')
        ) AS src("RoleName", "FunctionId", "CommandId")
        INNER JOIN "AppRoles" r ON r."NormalizedName" = src."RoleName"
        ON CONFLICT DO NOTHING;

        ------------------------------------------------------------
        -- Universities  (explicit Id → reset sequence sau)
        ------------------------------------------------------------
        INSERT INTO "Universities" ("Id", "Name", "City") VALUES
            (1,  'Đại học Bách khoa Hà Nội',      'Hà Nội'),
            (2,  'Đại học Công nghệ - ĐHQGHN',    'Hà Nội'),
            (3,  'Học viện Công nghệ BCVT',        'Hà Nội'),
            (4,  'Đại học FPT',                    'Hà Nội'),
            (5,  'Đại học KHTN - ĐHQG TP.HCM',    'TP.HCM'),
            (6,  'Đại học Duy Tân',                'Đà Nẵng'),
            (7,  'Đại học Thương Mại (TMU)',       'Hà Nội'),
            (8,  'Đại học Kinh tế Quốc dân',      'Hà Nội'),
            (9,  'Đại học Giao thông Vận tải',    'Hà Nội'),
            (10, 'Học viện Ngân hàng',             'Hà Nội'),
            (11, 'Học viện Kỹ thuật Mật mã',      'Hà Nội'),
            (12, 'Đại học Công nghiệp Hà Nội',    'Hà Nội'),
            (13, 'Đại học Sư phạm Kỹ thuật',      'TP.HCM'),
            (14, 'Đại học Bách khoa TP.HCM',      'TP.HCM'),
            (15, 'Đại học Tôn Đức Thắng',         'TP.HCM')
        ON CONFLICT ("Id") DO NOTHING;
        -- Reset sequence để auto-increment tiếp theo không bị conflict
        PERFORM setval(
            pg_get_serial_sequence('"Universities"', 'Id'),
            (SELECT MAX("Id") FROM "Universities")
        );

        ------------------------------------------------------------
        -- AppUsers bulk (37 users)
        -- Những user đã tạo bởi C# ở trên → WHEN MATCHED chỉ update
        -- các trường phi-security, KHÔNG đụng PasswordHash.
        -- Những user chưa có → INSERT với PasswordHash = 'HASH' (demo).
        ------------------------------------------------------------
        INSERT INTO "AppUsers" (
            "Id", "FirstName", "LastName", "IsActive", "CreateDate",
            "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
            "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
            "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled",
            "AccessFailedCount", "StudentId", "UniversityId", "GPA"
        ) VALUES
            -- Admin
            ('U_ADMIN_001',  'Admin',  'System',        TRUE, NOW(), 'admin',         'ADMIN',         'admin@deha.vn',    'ADMIN@DEHA.VN',    TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, NULL,    NULL, NULL),
            -- HR
            ('U_HR_001',     'Minh',   'Nguyễn HR',     TRUE, NOW(), 'hr.minh',       'HR.MINH',       'hr.minh@deha.vn',  'HR.MINH@DEHA.VN',  TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, NULL,    NULL, NULL),
            ('U_HR_002',     'Lan',    'Phạm HR',       TRUE, NOW(), 'hr.lan',        'HR.LAN',        'hr.lan@deha.vn',   'HR.LAN@DEHA.VN',   TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, NULL,    NULL, NULL),
            -- Mentor
            ('U_MENTOR_001', 'Hoàng',  'Backend',       TRUE, NOW(), 'mentor.hoang',  'MENTOR.HOANG',  'hoang@deha.vn',    'HOANG@DEHA.VN',    TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, NULL,    NULL, NULL),
            ('U_MENTOR_002', 'Anh',    'AI_NLP',        TRUE, NOW(), 'mentor.anh',    'MENTOR.ANH',    'anh@deha.vn',      'ANH@DEHA.VN',      TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, NULL,    NULL, NULL),
            ('U_MENTOR_003', 'Đức',    'QA_QC',         TRUE, NOW(), 'mentor.duc',    'MENTOR.DUC',    'duc@deha.vn',      'DUC@DEHA.VN',      TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, NULL,    NULL, NULL),
            ('U_MENTOR_004', 'Hương',  'BA_Lead',       TRUE, NOW(), 'mentor.huong',  'MENTOR.HUONG',  'huong@deha.vn',    'HUONG@DEHA.VN',    TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, NULL,    NULL, NULL),
            -- Intern
            ('U_INTERN_001', 'Thanh',  'Trần Phương',   TRUE, NOW(), 'intern.thanh',  'INTERN.THANH',  'thanh@sv.vn',      'THANH@SV.VN',      TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV001', 7,  3.85),
            ('U_INTERN_002', 'Nam',    'Nguyễn Hoài',   TRUE, NOW(), 'intern.nam',    'INTERN.NAM',    'nam@sv.vn',        'NAM@SV.VN',        TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV002', 2,  3.20),
            ('U_INTERN_003', 'Linh',   'Vũ Khánh',      TRUE, NOW(), 'intern.linh',   'INTERN.LINH',   'linh@sv.vn',       'LINH@SV.VN',       TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV003', 3,  3.72),
            ('U_INTERN_004', 'Dũng',   'Phạm Minh',     TRUE, NOW(), 'intern.dung',   'INTERN.DUNG',   'dung@sv.vn',       'DUNG@SV.VN',       TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV004', 4,  2.95),
            ('U_INTERN_005', 'Hà',     'Đỗ Ngọc',       TRUE, NOW(), 'intern.ha',     'INTERN.HA',     'ha@sv.vn',         'HA@SV.VN',         TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV005', 5,  3.10),
            ('U_INTERN_006', 'Quân',   'Bùi Anh',       TRUE, NOW(), 'intern.quan',   'INTERN.QUAN',   'quan@sv.vn',       'QUAN@SV.VN',       TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV006', 6,  3.60),
            ('U_INTERN_007', 'Hải',    'Trần Ngọc',     TRUE, NOW(), 'intern.hai',    'INTERN.HAI',    'hai@sv.vn',        'HAI@SV.VN',        TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV007', 7,  3.50),
            ('U_INTERN_008', 'Thảo',   'Lê Phương',     TRUE, NOW(), 'intern.thao',   'INTERN.THAO',   'thao@sv.vn',       'THAO@SV.VN',       TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV008', 8,  3.65),
            ('U_INTERN_009', 'Phong',  'Nguyễn Đình',   TRUE, NOW(), 'intern.phong',  'INTERN.PHONG',  'phong@sv.vn',      'PHONG@SV.VN',      TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV009', 1,  3.15),
            ('U_INTERN_010', 'Yến',    'Hoàng Hải',     TRUE, NOW(), 'intern.yen',    'INTERN.YEN',    'yen@sv.vn',        'YEN@SV.VN',        TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV010', 7,  3.80),
            ('U_INTERN_011', 'Cường',  'Vũ Quốc',       TRUE, NOW(), 'intern.cuong',  'INTERN.CUONG',  'cuong@sv.vn',      'CUONG@SV.VN',      TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV011', 9,  3.00),
            ('U_INTERN_012', 'Mai',    'Đặng Phương',   TRUE, NOW(), 'intern.mai',    'INTERN.MAI',    'mai@sv.vn',        'MAI@SV.VN',        TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV012', 10, 3.40),
            ('U_INTERN_013', 'Tuấn',   'Đinh Khắc',     TRUE, NOW(), 'intern.tuan',   'INTERN.TUAN',   'tuan@sv.vn',       'TUAN@SV.VN',       TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV013', 11, 2.80),
            ('U_INTERN_014', 'Trang',  'Lý Thu',        TRUE, NOW(), 'intern.trang',  'INTERN.TRANG',  'trang@sv.vn',      'TRANG@SV.VN',      TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV014', 12, 3.55),
            ('U_INTERN_015', 'Khoa',   'Hồ Đăng',       TRUE, NOW(), 'intern.khoa',   'INTERN.KHOA',   'khoa@sv.vn',       'KHOA@SV.VN',       TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV015', 13, 3.70),
            ('U_INTERN_016', 'Bình',   'Trương Thanh',  TRUE, NOW(), 'intern.binh',   'INTERN.BINH',   'binh@sv.vn',       'BINH@SV.VN',       TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV016', 14, 3.10),
            ('U_INTERN_017', 'Hoa',    'Ngô Quý',       TRUE, NOW(), 'intern.hoa',    'INTERN.HOA',    'hoa@sv.vn',        'HOA@SV.VN',        TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV017', 15, 3.25),
            ('U_INTERN_018', 'Việt',   'Bùi Quang',     TRUE, NOW(), 'intern.viet',   'INTERN.VIET',   'viet@sv.vn',       'VIET@SV.VN',       TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV018', 1,  2.90),
            ('U_INTERN_019', 'Giang',  'Phạm Hương',    TRUE, NOW(), 'intern.giang',  'INTERN.GIANG',  'giang@sv.vn',      'GIANG@SV.VN',      TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV019', 2,  3.65),
            ('U_INTERN_020', 'Đạt',    'Nguyễn Thành',  TRUE, NOW(), 'intern.dat',    'INTERN.DAT',    'dat@sv.vn',        'DAT@SV.VN',        TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV020', 3,  3.80),
            ('U_INTERN_021', 'My',     'Trần Trà',      TRUE, NOW(), 'intern.my',     'INTERN.MY',     'my@sv.vn',         'MY@SV.VN',         TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV021', 4,  3.40),
            ('U_INTERN_022', 'Long',   'Hoàng Phi',     TRUE, NOW(), 'intern.long',   'INTERN.LONG',   'long@sv.vn',       'LONG@SV.VN',       TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV022', 5,  2.85),
            ('U_INTERN_023', 'Hân',    'Đỗ Gia',        TRUE, NOW(), 'intern.han',    'INTERN.HAN',    'han@sv.vn',        'HAN@SV.VN',        TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV023', 6,  3.90),
            ('U_INTERN_024', 'Sơn',    'Lê Tùng',       TRUE, NOW(), 'intern.son',    'INTERN.SON',    'son@sv.vn',        'SON@SV.VN',        TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV024', 7,  3.05),
            ('U_INTERN_025', 'An',     'Vũ Bình',       TRUE, NOW(), 'intern.an',     'INTERN.AN',     'an@sv.vn',         'AN@SV.VN',         TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV025', 8,  3.35),
            ('U_INTERN_026', 'Phúc',   'Nguyễn Hồng',   TRUE, NOW(), 'intern.phuc',   'INTERN.PHUC',   'phuc@sv.vn',       'PHUC@SV.VN',       TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV026', 9,  3.15),
            ('U_INTERN_027', 'Tâm',    'Trần Minh',     TRUE, NOW(), 'intern.tam',    'INTERN.TAM',    'tam@sv.vn',        'TAM@SV.VN',        TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV027', 10, 3.45),
            ('U_INTERN_028', 'Khánh',  'Lý Quốc',       TRUE, NOW(), 'intern.khanh',  'INTERN.KHANH',  'khanh@sv.vn',      'KHANH@SV.VN',      TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV028', 11, 2.75),
            ('U_INTERN_029', 'Nhi',    'Đặng Yến',      TRUE, NOW(), 'intern.nhi',    'INTERN.NHI',    'nhi@sv.vn',        'NHI@SV.VN',        TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV029', 12, 3.60),
            ('U_INTERN_030', 'Bảo',    'Phạm Gia',      TRUE, NOW(), 'intern.bao',    'INTERN.BAO',    'bao@sv.vn',        'BAO@SV.VN',        TRUE, 'HASH', gen_random_uuid()::TEXT, gen_random_uuid()::TEXT, TRUE, FALSE, TRUE, 0, 'SV030', 13, 3.85)
        ON CONFLICT ("Id") DO UPDATE SET
            "FirstName"    = EXCLUDED."FirstName",
            "LastName"     = EXCLUDED."LastName",
            "IsActive"     = EXCLUDED."IsActive",
            "StudentId"    = EXCLUDED."StudentId",
            "UniversityId" = EXCLUDED."UniversityId",
            "GPA"          = EXCLUDED."GPA";
        -- ⚠️ PasswordHash KHÔNG được update ở đây → tránh ghi đè hash thực của C#

        ------------------------------------------------------------
        -- UserRoles
        ------------------------------------------------------------
        INSERT INTO "UserRoles" ("UserId", "RoleId")
        SELECT u."Id", r."Id"
        FROM "AppUsers" u
        INNER JOIN "AppRoles" r ON r."NormalizedName" = CASE
            WHEN u."Id" LIKE 'U_ADMIN%'  THEN 'ADMIN'
            WHEN u."Id" LIKE 'U_HR%'     THEN 'HR'
            WHEN u."Id" LIKE 'U_MENTOR%' THEN 'MENTOR'
            WHEN u."Id" LIKE 'U_INTERN%' THEN 'INTERN'
        END
        WHERE u."Id" LIKE 'U_%'
        ON CONFLICT ("UserId", "RoleId") DO NOTHING;

        ------------------------------------------------------------
        -- JobPositions
        ------------------------------------------------------------
        INSERT INTO "JobPositions" ("Id", "Title", "Description", "IsActive", "CreateDate") VALUES
            (1,  '.NET Backend',     'API, EF Core',             TRUE, NOW()),
            (2,  'Frontend',         'Web MVC, React',           TRUE, NOW()),
            (3,  'AI/NLP',           'Xử lý văn bản, TF-IDF',   TRUE, NOW()),
            (4,  'QA/QC',            'Test Case, k6',            TRUE, NOW()),
            (5,  'Business Analyst', 'FRS, Use Case',            TRUE, NOW()),
            (6,  'Mobile App',       'React Native, Flutter',    TRUE, NOW()),
            (7,  'Data Analyst',     'SQL, PowerBI',             TRUE, NOW()),
            (8,  'DevOps',           'CI/CD, Docker, AWS',       TRUE, NOW()),
            (9,  'UI/UX Design',     'Figma, Prototype',         TRUE, NOW()),
            (10, 'NodeJS Backend',   'Express, MongoDB',         TRUE, NOW())
        ON CONFLICT ("Id") DO NOTHING;
        PERFORM setval(
            pg_get_serial_sequence('"JobPositions"', 'Id'),
            (SELECT MAX("Id") FROM "JobPositions")
        );

        ------------------------------------------------------------
        -- JobDescriptions
        ------------------------------------------------------------
        INSERT INTO "JobDescriptions" (
            "Id", "JobPositionId", "Title", "DetailContent", "RequiredSkills",
            "MinGPA", "CreatedByUserId", "Status", "CreateDate", "DeadlineDate"
        ) VALUES
            (1,  1, 'JD .NET Backend - Đợt 1',  'Phát triển API bằng ASP.NET Core.',             'C#, ASP.NET Core, SQL Server', 3.0, 'U_HR_001', 'OPEN',   NOW(), NOW() + INTERVAL '30 days'),
            (2,  2, 'JD Frontend Web - Đợt 1',  'Thiết kế giao diện trên nền tảng Web.',         'HTML, CSS, JS, React',         2.8, 'U_HR_001', 'OPEN',   NOW(), NOW() + INTERVAL '30 days'),
            (3,  3, 'JD AI/NLP - Đợt 1',        'Nghiên cứu mô hình bóc tách dữ liệu văn bản.', 'Python, ML.NET, NLP',          3.2, 'U_HR_002', 'OPEN',   NOW(), NOW() + INTERVAL '45 days'),
            (4,  4, 'JD Tester/QA - Đợt 1',     'Viết kịch bản test, chạy script hiệu năng.',   'Manual Test, API Test, k6',    2.8, 'U_HR_001', 'OPEN',   NOW(), NOW() + INTERVAL '30 days'),
            (5,  5, 'JD BA Intern - Đợt 1',     'Lấy yêu cầu khách hàng, viết FRS.',            'UML, FRS, Use Case',           3.0, 'U_HR_002', 'OPEN',   NOW(), NOW() + INTERVAL '30 days'),
            (6,  6, 'JD Mobile React Native',    'Build ứng dụng mobile đa nền tảng.',           'React Native, JS/TS',          2.8, 'U_HR_001', 'OPEN',   NOW(), NOW() + INTERVAL '30 days'),
            (7,  7, 'JD Data Analyst Intern',    'Xử lý số liệu, làm Dashboard thống kê.',       'SQL, Python, PowerBI',         3.2, 'U_HR_002', 'OPEN',   NOW(), NOW() + INTERVAL '30 days'),
            (8,  8, 'JD DevOps Intern',          'Cấu hình môi trường, đẩy Docker lên AWS.',     'Linux, Docker, AWS ECR',       3.0, 'U_HR_001', 'OPEN',   NOW(), NOW() + INTERVAL '30 days'),
            (9,  9, 'JD UI/UX Intern',           'Làm Wireframe, Prototype cho module LMS.',     'Figma, UI/UX Principles',      2.8, 'U_HR_002', 'OPEN',   NOW(), NOW() + INTERVAL '30 days'),
            (10,10, 'JD NodeJS Backend',         'Phát triển API bằng NodeJS.',                  'NodeJS, Express, MongoDB',     3.0, 'U_HR_001', 'OPEN',   NOW(), NOW() + INTERVAL '30 days'),
            (11, 1, 'JD .NET Backend - Đợt 2',  'Xây dựng Microservices với C#.',               'C#, Microservices, Docker',    3.2, 'U_HR_001', 'OPEN',   NOW(), NOW() + INTERVAL '30 days'),
            (12, 5, 'JD BA Intern - Đợt 2',     'Tối ưu quy trình tuyển dụng thông minh.',     'BPMN, SQL Cơ bản, FRS',        3.0, 'U_HR_002', 'OPEN',   NOW(), NOW() + INTERVAL '30 days'),
            (13, 4, 'JD Automation QA',          'Làm Automation test bằng C# và Selenium.',    'Selenium, k6, C#',             3.0, 'U_HR_001', 'OPEN',   NOW(), NOW() + INTERVAL '30 days'),
            (14, 2, 'JD Frontend VueJS',         'Dùng VueJS làm giao diện Dashboard.',         'VueJS, TailwindCSS',           2.8, 'U_HR_001', 'CLOSED', NOW(), NOW() - INTERVAL '5 days'),
            (15, 8, 'JD Cloud/DevOps',           'Triển khai dự án lên nền tảng Azure.',        'Azure, Kubernetes',            3.5, 'U_HR_002', 'CLOSED', NOW(), NOW() - INTERVAL '5 days')
        ON CONFLICT ("Id") DO NOTHING;
        PERFORM setval(
            pg_get_serial_sequence('"JobDescriptions"', 'Id'),
            (SELECT MAX("Id") FROM "JobDescriptions")
        );

        ------------------------------------------------------------
        -- Applications
        ------------------------------------------------------------
        INSERT INTO "Applications" (
            "Id", "ApplicantUserId", "JobDescriptionId", "CVFileUrl",
            "CoverLetter", "ApplyDate", "Status"
        ) VALUES
            (1,  'U_INTERN_001', 1,  '/cv/1.pdf',  'Xin ứng tuyển vào vị trí Backend.',  NOW() - INTERVAL '15 days', 'ACCEPTED'),
            (2,  'U_INTERN_002', 1,  '/cv/2.pdf',  'Xin ứng tuyển vào vị trí Backend.',  NOW() - INTERVAL '14 days', 'INTERVIEW'),
            (3,  'U_INTERN_003', 3,  '/cv/3.pdf',  'Xin ứng tuyển vào vị trí AI NLP.',   NOW() - INTERVAL '13 days', 'ACCEPTED'),
            (4,  'U_INTERN_004', 2,  '/cv/4.pdf',  'Xin ứng tuyển vào vị trí Frontend.', NOW() - INTERVAL '12 days', 'PENDING'),
            (5,  'U_INTERN_005', 2,  '/cv/5.pdf',  'Xin ứng tuyển vào vị trí Frontend.', NOW() - INTERVAL '11 days', 'REJECTED'),
            (6,  'U_INTERN_006', 3,  '/cv/6.pdf',  'Xin ứng tuyển vào vị trí AI NLP.',   NOW() - INTERVAL '10 days', 'SCREENING'),
            (7,  'U_INTERN_007', 4,  '/cv/7.pdf',  'Xin ứng tuyển vào vị trí QA/QC.',    NOW() - INTERVAL '10 days', 'ACCEPTED'),
            (8,  'U_INTERN_008', 5,  '/cv/8.pdf',  'Xin ứng tuyển vào vị trí BA.',       NOW() - INTERVAL '9 days',  'ACCEPTED'),
            (9,  'U_INTERN_009', 1,  '/cv/9.pdf',  'Xin ứng tuyển vào vị trí Backend.',  NOW() - INTERVAL '8 days',  'SCREENING'),
            (10, 'U_INTERN_010', 5,  '/cv/10.pdf', 'Xin ứng tuyển vào vị trí BA.',       NOW() - INTERVAL '7 days',  'INTERVIEW'),
            (11, 'U_INTERN_011', 6,  '/cv/11.pdf', 'Xin ứng tuyển vào vị trí Mobile.',   NOW() - INTERVAL '6 days',  'ACCEPTED'),
            (12, 'U_INTERN_012', 7,  '/cv/12.pdf', 'Xin ứng tuyển vào vị trí Data.',     NOW() - INTERVAL '5 days',  'PENDING'),
            (13, 'U_INTERN_013', 8,  '/cv/13.pdf', 'Xin ứng tuyển vào vị trí DevOps.',   NOW() - INTERVAL '4 days',  'INTERVIEW'),
            (14, 'U_INTERN_014', 9,  '/cv/14.pdf', 'Xin ứng tuyển vào vị trí UI/UX.',    NOW() - INTERVAL '3 days',  'ACCEPTED'),
            (15, 'U_INTERN_015', 10, '/cv/15.pdf', 'Xin ứng tuyển vào vị trí NodeJS.',   NOW() - INTERVAL '2 days',  'ACCEPTED'),
            (16, 'U_INTERN_016', 1,  '/cv/16.pdf', 'Xin ứng tuyển vào vị trí Backend.',  NOW() - INTERVAL '15 days', 'REJECTED'),
            (17, 'U_INTERN_017', 2,  '/cv/17.pdf', 'Xin ứng tuyển vào vị trí Frontend.', NOW() - INTERVAL '14 days', 'SCREENING'),
            (18, 'U_INTERN_018', 3,  '/cv/18.pdf', 'Xin ứng tuyển vào vị trí AI NLP.',   NOW() - INTERVAL '13 days', 'ACCEPTED'),
            (19, 'U_INTERN_019', 4,  '/cv/19.pdf', 'Xin ứng tuyển vào vị trí QA/QC.',    NOW() - INTERVAL '12 days', 'PENDING'),
            (20, 'U_INTERN_020', 5,  '/cv/20.pdf', 'Xin ứng tuyển vào vị trí BA.',       NOW() - INTERVAL '11 days', 'INTERVIEW'),
            (21, 'U_INTERN_021', 6,  '/cv/21.pdf', 'Xin ứng tuyển vào vị trí Mobile.',   NOW() - INTERVAL '10 days', 'ACCEPTED'),
            (22, 'U_INTERN_022', 7,  '/cv/22.pdf', 'Xin ứng tuyển vào vị trí Data.',     NOW() - INTERVAL '9 days',  'REJECTED'),
            (23, 'U_INTERN_023', 8,  '/cv/23.pdf', 'Xin ứng tuyển vào vị trí DevOps.',   NOW() - INTERVAL '8 days',  'ACCEPTED'),
            (24, 'U_INTERN_024', 9,  '/cv/24.pdf', 'Xin ứng tuyển vào vị trí UI/UX.',    NOW() - INTERVAL '7 days',  'SCREENING'),
            (25, 'U_INTERN_025', 10, '/cv/25.pdf', 'Xin ứng tuyển vào vị trí NodeJS.',   NOW() - INTERVAL '6 days',  'PENDING'),
            (26, 'U_INTERN_026', 11, '/cv/26.pdf', 'Xin ứng tuyển Backend đợt 2.',        NOW() - INTERVAL '5 days',  'ACCEPTED'),
            (27, 'U_INTERN_027', 12, '/cv/27.pdf', 'Xin ứng tuyển BA đợt 2.',             NOW() - INTERVAL '4 days',  'INTERVIEW'),
            (28, 'U_INTERN_028', 13, '/cv/28.pdf', 'Xin ứng tuyển QA Automation.',        NOW() - INTERVAL '3 days',  'ACCEPTED'),
            (29, 'U_INTERN_029', 4,  '/cv/29.pdf', 'Xin ứng tuyển vào vị trí QA/QC.',    NOW() - INTERVAL '2 days',  'SCREENING'),
            (30, 'U_INTERN_030', 5,  '/cv/30.pdf', 'Xin ứng tuyển vào vị trí BA.',       NOW() - INTERVAL '1 day',   'ACCEPTED')
        ON CONFLICT ("Id") DO NOTHING;
        PERFORM setval(
            pg_get_serial_sequence('"Applications"', 'Id'),
            (SELECT MAX("Id") FROM "Applications")
        );

        ------------------------------------------------------------
        -- CVParsedDatas  (insert nếu chưa có theo ApplicationId)
        ------------------------------------------------------------
        INSERT INTO "CVParsedDatas" (
            "Id", "ApplicationId", "FullName", "EmailExtracted",
            "SkillsExtracted", "RawText", "ParsedAt"
        )
        SELECT
            a."Id",
            a."Id",
            'Ứng viên ' || a."Id"::TEXT,
            'uv' || a."Id"::TEXT || '@gmail.com',
            'C#, SQL, Git, UML, Postman, k6',
            'Dữ liệu CV bóc tách thô...',
            NOW()
        FROM "Applications" a
        WHERE NOT EXISTS (
            SELECT 1 FROM "CVParsedDatas" cv WHERE cv."ApplicationId" = a."Id"
        );
        PERFORM setval(
            pg_get_serial_sequence('"CVParsedDatas"', 'Id'),
            (SELECT MAX("Id") FROM "CVParsedDatas")
        );

        ------------------------------------------------------------
        -- AIScreeningResults  (insert nếu chưa có theo ApplicationId)
        ------------------------------------------------------------
        INSERT INTO "AIScreeningResults" (
            "Id", "ApplicationId", "MatchingScore", "Ranking",
            "KeywordsMatched", "KeywordsMissing", "ProcessingStatus",
            "ScreenedAt", "ReviewedByHRId"
        )
        SELECT
            a."Id",
            a."Id",
            CAST(60.0 + (a."Id" % 40) AS NUMERIC(5,2)),
            (a."Id" % 3) + 1,
            'C#, SQL, Git',
            'Docker, Azure',
            'Completed',
            NOW(),
            CASE WHEN a."Id" % 2 = 0 THEN 'U_HR_001' ELSE 'U_HR_002' END
        FROM "Applications" a
        WHERE NOT EXISTS (
            SELECT 1 FROM "AIScreeningResults" ai WHERE ai."ApplicationId" = a."Id"
        );
        PERFORM setval(
            pg_get_serial_sequence('"AIScreeningResults"', 'Id'),
            (SELECT MAX("Id") FROM "AIScreeningResults")
        );

        ------------------------------------------------------------
        -- Courses
        ------------------------------------------------------------
        INSERT INTO "Courses" ("Id", "Title", "Level", "CreatedByUserId", "IsPublished", "CreateDate") VALUES
            (1,  'ASP.NET Core Web API', 'BEGINNER',     'U_MENTOR_001', TRUE, NOW()),
            (2,  'EF Core & SQL',         'INTERMEDIATE', 'U_MENTOR_001', TRUE, NOW()),
            (3,  'NLP CV Screening',      'INTERMEDIATE', 'U_MENTOR_002', TRUE, NOW()),
            (4,  'Docker & AWS ECR',      'ADVANCED',     'U_MENTOR_001', TRUE, NOW()),
            (5,  'Test k6 Performance',   'INTERMEDIATE', 'U_MENTOR_003', TRUE, NOW()),
            (6,  'BA Thực chiến',         'BEGINNER',     'U_MENTOR_004', TRUE, NOW()),
            (7,  'React Native Cơ bản',   'BEGINNER',     'U_MENTOR_001', TRUE, NOW()),
            (8,  'PowerBI Dashboard',     'INTERMEDIATE', 'U_MENTOR_002', TRUE, NOW()),
            (9,  'CI/CD Github Actions',  'ADVANCED',     'U_MENTOR_001', TRUE, NOW()),
            (10, 'Agile Scrum 101',       'BEGINNER',     'U_MENTOR_004', TRUE, NOW())
        ON CONFLICT ("Id") DO NOTHING;
        PERFORM setval(
            pg_get_serial_sequence('"Courses"', 'Id'),
            (SELECT MAX("Id") FROM "Courses")
        );

        ------------------------------------------------------------
        -- CourseChapters
        ------------------------------------------------------------
        INSERT INTO "CourseChapters" ("Id", "CourseId", "Title", "SortOrder") VALUES
            (1,1,'Chương 1',1),(2,2,'Chương 1',1),(3,3,'Chương 1',1),(4,4,'Chương 1',1),(5,5,'Chương 1',1),
            (6,6,'Chương 1',1),(7,7,'Chương 1',1),(8,8,'Chương 1',1),(9,9,'Chương 1',1),(10,10,'Chương 1',1)
        ON CONFLICT ("Id") DO NOTHING;
        PERFORM setval(
            pg_get_serial_sequence('"CourseChapters"', 'Id'),
            (SELECT MAX("Id") FROM "CourseChapters")
        );

        ------------------------------------------------------------
        -- Lessons
        ------------------------------------------------------------
        INSERT INTO "Lessons" ("Id","ChapterId","Title","LessonType","DurationMinutes","SortOrder","IsRequired") VALUES
            (1, 1, 'REST API',        'VIDEO',    20, 1, TRUE), (2, 1,  'JWT Auth',     'DOCUMENT', 30, 2, TRUE),
            (3, 2, 'Migration',       'VIDEO',    25, 1, TRUE), (4, 2,  'LINQ',         'VIDEO',    35, 2, TRUE),
            (5, 3, 'TF-IDF',          'DOCUMENT', 40, 1, TRUE), (6, 3,  'Cosine Sim',  'VIDEO',    30, 2, TRUE),
            (7, 4, 'Dockerfile',      'VIDEO',    20, 1, TRUE), (8, 4,  'AWS ECR',      'DOCUMENT', 25, 2, TRUE),
            (9, 5, 'k6 Scripting',    'VIDEO',    45, 1, TRUE), (10, 5, 'VU config',    'DOCUMENT', 20, 2, TRUE),
            (11,6, 'Viết Use Case',   'VIDEO',    30, 1, TRUE), (12, 6, 'Viết FRS',     'DOCUMENT', 40, 2, TRUE),
            (13,7, 'React Hooks',     'VIDEO',    25, 1, TRUE), (14, 7, 'Redux',         'VIDEO',    35, 2, TRUE),
            (15,8, 'DAX Filter',      'DOCUMENT', 30, 1, TRUE), (16, 8, 'Data Viz',     'VIDEO',    20, 2, TRUE),
            (17,9, 'YAML file',       'VIDEO',    25, 1, TRUE), (18, 9, 'Runners',       'DOCUMENT', 30, 2, TRUE),
            (19,10,'Sprint Plan',     'VIDEO',    20, 1, TRUE), (20,10, 'Retro',         'DOCUMENT', 25, 2, TRUE)
        ON CONFLICT ("Id") DO NOTHING;
        PERFORM setval(
            pg_get_serial_sequence('"Lessons"', 'Id'),
            (SELECT MAX("Id") FROM "Lessons")
        );

        ------------------------------------------------------------
        -- Enrollments  (composite PK: InternUserId + CourseId)
        ------------------------------------------------------------
        INSERT INTO "Enrollments" ("InternUserId", "CourseId", "EnrollDate", "CompletionPercent") VALUES
            ('U_INTERN_001',1, NOW(),100),('U_INTERN_001',4, NOW(),100),('U_INTERN_002',2, NOW(),50),
            ('U_INTERN_003',3, NOW(),80), ('U_INTERN_004',1, NOW(),10), ('U_INTERN_006',3, NOW(),95),
            ('U_INTERN_007',5, NOW(),100),('U_INTERN_008',6, NOW(),90), ('U_INTERN_009',1, NOW(),50),
            ('U_INTERN_010',6, NOW(),80), ('U_INTERN_011',7, NOW(),30), ('U_INTERN_012',8, NOW(),60),
            ('U_INTERN_013',9, NOW(),100),('U_INTERN_014',10,NOW(),100),('U_INTERN_015',10,NOW(),0),
            ('U_INTERN_016',1, NOW(),10), ('U_INTERN_017',2, NOW(),25), ('U_INTERN_018',3, NOW(),10),
            ('U_INTERN_019',5, NOW(),65), ('U_INTERN_020',6, NOW(),45), ('U_INTERN_021',7, NOW(),85),
            ('U_INTERN_022',8, NOW(),30), ('U_INTERN_023',4, NOW(),100),('U_INTERN_024',9, NOW(),40),
            ('U_INTERN_025',10,NOW(),100),('U_INTERN_026',1, NOW(),70), ('U_INTERN_027',1, NOW(),0),
            ('U_INTERN_028',5, NOW(),20), ('U_INTERN_029',4, NOW(),15), ('U_INTERN_030',6, NOW(),100)
        ON CONFLICT ("InternUserId", "CourseId") DO UPDATE SET
            "CompletionPercent" = EXCLUDED."CompletionPercent";

        ------------------------------------------------------------
        -- InternshipPeriods
        ------------------------------------------------------------
        INSERT INTO "InternshipPeriods" ("Id", "Name", "StartDate", "EndDate", "IsActive") VALUES
            (1, 'Kỳ thực tập Spring 2026', NOW() - INTERVAL '30 days', NOW() + INTERVAL '60 days', TRUE)
        ON CONFLICT ("Id") DO NOTHING;
        PERFORM setval(
            pg_get_serial_sequence('"InternshipPeriods"', 'Id'),
            (SELECT MAX("Id") FROM "InternshipPeriods")
        );

        ------------------------------------------------------------
        -- InternAssignments
        -- InternAssignments có auto-generated Id (int PK).
        -- Unique constraint trên (InternUserId, PeriodId) → dùng WHERE NOT EXISTS
        -- để an toàn kể cả khi chưa có unique index.
        ------------------------------------------------------------
        INSERT INTO "InternAssignments" ("InternUserId", "MentorUserId", "PeriodId", "AssignedDate")
        SELECT
            io.intern_id,
            CASE (io.rn % 4)
                WHEN 1 THEN 'U_MENTOR_001'
                WHEN 2 THEN 'U_MENTOR_002'
                WHEN 3 THEN 'U_MENTOR_003'
                ELSE        'U_MENTOR_004'
            END,
            ip."Id",
            NOW()
        FROM (
            SELECT ROW_NUMBER() OVER (ORDER BY u."Id") AS rn, u."Id" AS intern_id
            FROM "AppUsers" u
            WHERE u."Id" LIKE 'U_INTERN%'
        ) io
        CROSS JOIN "InternshipPeriods" ip
        WHERE ip."Name" = 'Kỳ thực tập Spring 2026'
          AND NOT EXISTS (
              SELECT 1 FROM "InternAssignments" ia
              WHERE ia."InternUserId" = io.intern_id AND ia."PeriodId" = ip."Id"
          );

        ------------------------------------------------------------
        -- TaskItems
        -- Dùng WHERE NOT EXISTS (AssignmentId + Title) để idempotent
        -- mà không cần unique index trên 2 cột đó.
        ------------------------------------------------------------
        INSERT INTO "TaskItems" (
            "Title", "Description", "AssignmentId",
            "Priority", "Status", "Deadline",
            "EstimatedHours", "CreateDate", "CreatedByUserId"
        )
        SELECT
            ts.title, ts.description, ia."Id",
            ts.priority, ts.status,
            NOW() + (ts.deadline_days || ' days')::INTERVAL,
            ts.hours, NOW(), ts.creator
        FROM (VALUES
            (1, 'API JWT',                    'Login endpoint',       'HIGH',   'DONE',         2,  10, 'U_MENTOR_001'),
            (1, 'Docker AWS',                 'Push image ECR',       'HIGH',   'DONE',         3,  12, 'U_MENTOR_001'),
            (2, 'TF-IDF CV',                  'Extract skills',       'HIGH',   'IN_PROGRESS',  5,  16, 'U_MENTOR_002'),
            (3, 'k6 Load Test',               '100 VUs login',        'MEDIUM', 'DONE',         1,   8, 'U_MENTOR_003'),
            (4, 'Use Case Quản lý thực đơn',  'Vẽ bằng Draw.io',     'HIGH',   'DONE',         4,  10, 'U_MENTOR_004'),
            (5, 'UI 44px touch',              'Fix mobile UI',        'MEDIUM', 'TODO',         6,   6, 'U_MENTOR_001'),
            (6, 'Migration DB',               'LMS module',           'HIGH',   'IN_PROGRESS',  2,  14, 'U_MENTOR_001'),
            (7, 'Cosine Sim',                 'Matching AI',          'HIGH',   'TODO',         8,  20, 'U_MENTOR_002'),
            (8, 'Test API',                   'Postman runner',       'LOW',    'DONE',         1,   4, 'U_MENTOR_003'),
            (9, 'FRS Document',               'Phần Đăng nhập',       'HIGH',   'IN_PROGRESS',  3,  16, 'U_MENTOR_004'),
            (10,'Fix Bug #101',               'Fix crash login',      'HIGH',   'DONE',         2,   4, 'U_MENTOR_001'),
            (11,'Data Crawl',                 'Crawl JDs',            'MEDIUM', 'IN_PROGRESS',  4,  10, 'U_MENTOR_002'),
            (12,'Automation UI',              'Selenium script',      'HIGH',   'TODO',         6,  12, 'U_MENTOR_003'),
            (13,'BPMN Quy trình',             'Tuyển dụng flow',      'MEDIUM', 'DONE',         1,   8, 'U_MENTOR_004'),
            (14,'Redis Cache',                'Cache API',            'HIGH',   'TODO',         5,  12, 'U_MENTOR_001'),
            (15,'Log ELK',                    'Config Kibana',        'MEDIUM', 'IN_PROGRESS',  7,  16, 'U_MENTOR_001'),
            (16,'Model Train',                'Train ML.NET',         'HIGH',   'TODO',        10,  24, 'U_MENTOR_002'),
            (17,'Jmeter Test',                'Load test LMS',        'MEDIUM', 'DONE',        -1,   8, 'U_MENTOR_003'),
            (18,'UML Class',                  'Class Diagram',        'LOW',    'IN_PROGRESS',  2,   6, 'U_MENTOR_004'),
            (19,'React Router',               'Setup routes',         'HIGH',   'DONE',         1,   4, 'U_MENTOR_001'),
            (20,'Clean Data',                 'Remove stopwords',     'MEDIUM', 'TODO',         3,   8, 'U_MENTOR_002'),
            (21,'Bug report',                 'Log Jira',             'LOW',    'DONE',         0,   2, 'U_MENTOR_003'),
            (22,'Wireframe',                  'Dashboard UI',         'HIGH',   'IN_PROGRESS',  5,  12, 'U_MENTOR_004'),
            (23,'CI/CD Pipeline',             'Github Actions',       'HIGH',   'TODO',         7,  16, 'U_MENTOR_001'),
            (24,'Chatbot AI',                 'Dialogflow',           'MEDIUM', 'IN_PROGRESS',  9,  20, 'U_MENTOR_002'),
            (25,'Pen Test',                   'OWASP Top 10',         'HIGH',   'TODO',        12,  24, 'U_MENTOR_003'),
            (26,'User Story',                 'Sprint 1',             'MEDIUM', 'DONE',        -2,   8, 'U_MENTOR_004'),
            (27,'Entity Rel',                 'ERD Diagram',          'HIGH',   'IN_PROGRESS',  1,   6, 'U_MENTOR_001'),
            (28,'AWS S3',                     'Upload CV API',        'MEDIUM', 'TODO',         4,  10, 'U_MENTOR_001'),
            (29,'Review Doc',                 'Peer review',          'LOW',    'DONE',        -1,   4, 'U_MENTOR_004')
        ) AS ts(rn, title, description, priority, status, deadline_days, hours, creator)
        INNER JOIN (
            SELECT ROW_NUMBER() OVER (ORDER BY u."Id") AS rn, u."Id" AS intern_id
            FROM "AppUsers" u
            WHERE u."Id" LIKE 'U_INTERN%'
        ) io ON io.rn = ts.rn
        INNER JOIN "InternAssignments" ia
            ON ia."InternUserId" = io.intern_id
           AND ia."PeriodId" = (
               SELECT "Id" FROM "InternshipPeriods" WHERE "Name" = 'Kỳ thực tập Spring 2026' LIMIT 1
           )
        WHERE NOT EXISTS (
            SELECT 1 FROM "TaskItems" t
            WHERE t."AssignmentId" = ia."Id" AND t."Title" = ts.title
        );

        ------------------------------------------------------------
        -- DailyReports
        -- DELETE seed cũ (theo content) rồi INSERT lại → luôn fresh
        ------------------------------------------------------------
        DELETE FROM "DailyReports"
        WHERE "ReportDate"::DATE = v_report_date
          AND "InternUserId" LIKE 'U_INTERN_%'
          AND "Content" IN (
            'Đẩy xong Docker image lên ECR', 'Migration DB bị lỗi FK',
            'Bóc tách được 50 từ khóa CV',   'Làm mockup Figma',
            'Học OWASP',                      'Viết User Story',
            'Chạy script k6 pass 100 VUs',    'Vẽ xong Use Case Quản lý thực đơn',
            'Test API login bằng Postman',     'Vẽ ERD',
            'Code React component',            'Crawl data JD',
            'Viết script Selenium',            'Vẽ BPMN',
            'Đọc tài liệu Agile',             'Setup S3 bucket',
            'Đọc tài liệu',                   'Tìm hiểu Redis',
            'Setup ELK',                       'Tìm hiểu ML.NET',
            'Chạy Jmeter',                    'Vẽ Class Diagram',
            'Setup xong Github Actions',       'Code React Router',
            'Clear stopwords',                 'Log bug lên Jira',
            'Làm Wireframe',                   'Build pipeline',
            'Nghiên cứu Dialogflow',           'Peer review FRS của team'
          );

        INSERT INTO "DailyReports" (
            "InternUserId", "ReportDate", "Content",
            "PlannedTomorrow", "MentorFeedback", "ReviewedByMentorId"
        ) VALUES
            ('U_INTERN_001', v_report_date, 'Đẩy xong Docker image lên ECR',      'Tích hợp ECS',     'Tốt, nhớ check IAM policy',    'U_MENTOR_001'),
            ('U_INTERN_002', v_report_date, 'Migration DB bị lỗi FK',              'Fix lỗi cascade',  'Xem lại ERD',                  'U_MENTOR_001'),
            ('U_INTERN_003', v_report_date, 'Bóc tách được 50 từ khóa CV',         'Tính TF-IDF',      'Cần lọc thêm stopwords',       'U_MENTOR_002'),
            ('U_INTERN_004', v_report_date, 'Làm mockup Figma',                    'Xin review',       'Cần chú ý 44px touch target',  'U_MENTOR_004'),
            ('U_INTERN_005', v_report_date, 'Học OWASP',                           'Test SQLi',        'Ok',                           'U_MENTOR_003'),
            ('U_INTERN_006', v_report_date, 'Viết User Story',                     'Estimation',       'Ok',                           'U_MENTOR_004'),
            ('U_INTERN_007', v_report_date, 'Chạy script k6 pass 100 VUs',         'Report kết quả',   'Kiểm tra lại RAM usage',       'U_MENTOR_003'),
            ('U_INTERN_008', v_report_date, 'Vẽ xong Use Case Quản lý thực đơn',  'Viết FRS',         'Logic đúng chuẩn',             'U_MENTOR_004'),
            ('U_INTERN_009', v_report_date, 'Test API login bằng Postman',         'Test API Register','Ok',                           'U_MENTOR_003'),
            ('U_INTERN_010', v_report_date, 'Vẽ ERD',                              'Migration',        'Ok',                           'U_MENTOR_001'),
            ('U_INTERN_011', v_report_date, 'Code React component',                'Ghép API',         'Ok',                           'U_MENTOR_001'),
            ('U_INTERN_012', v_report_date, 'Crawl data JD',                       'Clean data',       'Ok',                           'U_MENTOR_002'),
            ('U_INTERN_013', v_report_date, 'Viết script Selenium',                'Chạy thử',         'Ok',                           'U_MENTOR_003'),
            ('U_INTERN_014', v_report_date, 'Vẽ BPMN',                             'Review',           'Ok',                           'U_MENTOR_004'),
            ('U_INTERN_015', v_report_date, 'Đọc tài liệu Agile',                 'Học Jira',         'Tốt',                          'U_MENTOR_004'),
            ('U_INTERN_016', v_report_date, 'Setup S3 bucket',                     'Code API',         'Ok',                           'U_MENTOR_001'),
            ('U_INTERN_017', v_report_date, 'Đọc tài liệu',                        'Peer review',      'Ok',                           'U_MENTOR_004'),
            ('U_INTERN_018', v_report_date, 'Tìm hiểu Redis',                      'Code demo',        'Ok',                           'U_MENTOR_001'),
            ('U_INTERN_019', v_report_date, 'Setup ELK',                           'Config Logstash',  'Ok',                           'U_MENTOR_001'),
            ('U_INTERN_020', v_report_date, 'Tìm hiểu ML.NET',                    'Train model',      'Ok',                           'U_MENTOR_002'),
            ('U_INTERN_021', v_report_date, 'Chạy Jmeter',                         'Đọc report',       'Ok',                           'U_MENTOR_003'),
            ('U_INTERN_022', v_report_date, 'Vẽ Class Diagram',                    'Code entity',      'Ok',                           'U_MENTOR_004'),
            ('U_INTERN_023', v_report_date, 'Setup xong Github Actions',           'Test push code',   'Tốt',                          'U_MENTOR_001'),
            ('U_INTERN_024', v_report_date, 'Code React Router',                   'Code UI',          'Ok',                           'U_MENTOR_001'),
            ('U_INTERN_025', v_report_date, 'Clear stopwords',                     'NLP',              'Ok',                           'U_MENTOR_002'),
            ('U_INTERN_026', v_report_date, 'Log bug lên Jira',                    'Verify bug',       'Ok',                           'U_MENTOR_003'),
            ('U_INTERN_027', v_report_date, 'Làm Wireframe',                       'UI design',        'Ok',                           'U_MENTOR_004'),
            ('U_INTERN_028', v_report_date, 'Build pipeline',                      'Deploy test',      'Ok',                           'U_MENTOR_001'),
            ('U_INTERN_029', v_report_date, 'Nghiên cứu Dialogflow',               'Tạo bot',          'Ok',                           'U_MENTOR_002'),
            ('U_INTERN_030', v_report_date, 'Peer review FRS của team',            'Hoàn thiện docs',  'Review kỹ phần rule',          'U_MENTOR_004');

        RAISE NOTICE '✅ AIMS seed data upserted successfully.';

        EXCEPTION
            WHEN OTHERS THEN
                RAISE EXCEPTION '❌ Seed failed: %  DETAIL: %', SQLERRM, SQLSTATE;
        END;
        $$;
        """;
}