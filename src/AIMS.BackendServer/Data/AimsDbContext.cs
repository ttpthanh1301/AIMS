using AIMS.BackendServer.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AIMS.BackendServer.Data;

public class AimsDbContext : IdentityDbContext<AppUser, AppRole, string>
{
    public AimsDbContext(DbContextOptions<AimsDbContext> options) : base(options) { }

    // ── Phân quyền ──────────────────────────────
    public DbSet<University> Universities { get; set; }
    public DbSet<Function> Functions { get; set; }
    public DbSet<Command> Commands { get; set; }
    public DbSet<CommandInFunction> CommandInFunctions { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }

    // ── Tuyển dụng ──────────────────────────────
    public DbSet<JobPosition> JobPositions { get; set; }
    public DbSet<JobDescription> JobDescriptions { get; set; }
    public DbSet<Application> Applications { get; set; }
    public DbSet<CVParsedData> CVParsedDatas { get; set; }
    public DbSet<AIScreeningResult> AIScreeningResults { get; set; }

    // ── LMS ─────────────────────────────────────
    public DbSet<Course> Courses { get; set; }
    public DbSet<CourseChapter> CourseChapters { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<LessonProgress> LessonProgresses { get; set; }
    public DbSet<QuizBank> QuizBanks { get; set; }
    public DbSet<QuizQuestion> QuizQuestions { get; set; }
    public DbSet<QuestionOption> QuestionOptions { get; set; }
    public DbSet<UserQuizAttempt> UserQuizAttempts { get; set; }
    public DbSet<UserQuizAnswer> UserQuizAnswers { get; set; }
    public DbSet<Certificate> Certificates { get; set; }

    // ── Task Management ─────────────────────────
    public DbSet<InternshipPeriod> InternshipPeriods { get; set; }
    public DbSet<InternAssignment> InternAssignments { get; set; }
    public DbSet<TaskItem> TaskItems { get; set; }
    public DbSet<TaskActivity> TaskActivities { get; set; }
    public DbSet<DailyReport> DailyReports { get; set; }
    public DbSet<Entities.Timesheet> Timesheets { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Đổi tên bảng Identity mặc định
        builder.Entity<AppUser>().ToTable("AppUsers");
        builder.Entity<AppRole>().ToTable("AppRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("UserTokens");

        // Áp dụng tất cả Fluent API configurations
        builder.ApplyConfigurationsFromAssembly(typeof(AimsDbContext).Assembly);
    }
}