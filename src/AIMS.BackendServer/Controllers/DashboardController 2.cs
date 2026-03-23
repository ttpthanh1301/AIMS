using AIMS.BackendServer.Data;
using AIMS.BackendServer.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AimsDbContext _context;

    public DashboardController(AimsDbContext context)
        => _context = context;

    [HttpGet("intern")]
    [Authorize(Roles = "Intern")]
    public async Task<IActionResult> GetInternDashboard()
    {
        var userId = User.GetUserId();  // ⭐

        var tasks = await _context.TaskItems
            .Include(t => t.Assignment)
            .Include(t => t.Timesheets)
            .Where(t => t.Assignment.InternUserId == userId)
            .ToListAsync();

        var since = DateTime.UtcNow.AddDays(-7).Date;
        var reports = await _context.DailyReports
            .CountAsync(r => r.InternUserId == userId && r.ReportDate >= since);

        var enrollments = await _context.Enrollments
            .Where(e => e.InternUserId == userId)
            .ToListAsync();

        var certCount = await _context.Certificates
            .CountAsync(c => c.InternUserId == userId);

        var now = DateTime.UtcNow;

        return Ok(new
        {
            Tasks = new
            {
                Total = tasks.Count,
                Todo = tasks.Count(t => t.Status == "TODO"),
                InProgress = tasks.Count(t => t.Status == "IN_PROGRESS"),
                Done = tasks.Count(t => t.Status == "DONE"),
                Overdue = tasks.Count(t => t.Deadline < now && t.Status != "DONE"),
                CompletionRate = tasks.Count == 0 ? 0 :
                    Math.Round(tasks.Count(t => t.Status == "DONE") * 100.0 / tasks.Count, 1),
                TotalHoursLogged = tasks.SelectMany(t => t.Timesheets)
                    .Sum(ts => ts.HoursWorked),
            },
            LMS = new
            {
                CoursesEnrolled = enrollments.Count,
                CoursesCompleted = enrollments.Count(e => e.CompletionPercent >= 100),
                AvgProgress = enrollments.Count == 0 ? 0 :
                    Math.Round((double)enrollments.Average(e => e.CompletionPercent), 1),
                Certificates = certCount,
            },
            DailyReports = new
            {
                Last7Days = reports,
                Streak = $"{reports}/7 ngày",
            },
            UpcomingDeadlines = tasks
                .Where(t => t.Status != "DONE" &&
                            t.Deadline > now &&
                            t.Deadline <= now.AddDays(3))
                .OrderBy(t => t.Deadline)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Priority,
                    t.Deadline,
                    DaysLeft = (t.Deadline - now).Days,
                })
                .ToList(),
        });
    }

    [HttpGet("mentor")]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> GetMentorDashboard()
    {
        var mentorId = User.GetUserId();  // ⭐

        var assignments = await _context.InternAssignments
            .Include(a => a.InternUser)
            .Include(a => a.Period)
            .Include(a => a.Tasks)
                .ThenInclude(t => t.Timesheets)
            .Where(a => a.MentorUserId == mentorId && a.Period.IsActive)
            .ToListAsync();

        var internStats = assignments.Select(a => new
        {
            InternId = a.InternUserId,
            InternName = $"{a.InternUser.FirstName} {a.InternUser.LastName}",
            InternEmail = a.InternUser.Email,
            TotalTasks = a.Tasks.Count,
            TodoTasks = a.Tasks.Count(t => t.Status == "TODO"),
            InProgressTasks = a.Tasks.Count(t => t.Status == "IN_PROGRESS"),
            DoneTasks = a.Tasks.Count(t => t.Status == "DONE"),
            OverdueTasks = a.Tasks.Count(t =>
                t.Deadline < DateTime.UtcNow && t.Status != "DONE"),
            CompletionRate = a.Tasks.Count == 0 ? 0 :
                Math.Round(a.Tasks.Count(t => t.Status == "DONE") * 100.0
                    / a.Tasks.Count, 1),
            TotalHours = a.Tasks
                .SelectMany(t => t.Timesheets)
                .Sum(ts => ts.HoursWorked),
        }).ToList();

        return Ok(new
        {
            Summary = new
            {
                TotalInterns = assignments.Count,
                TotalTasks = internStats.Sum(i => i.TotalTasks),
                TotalDone = internStats.Sum(i => i.DoneTasks),
                TotalOverdue = internStats.Sum(i => i.OverdueTasks),
                AvgCompletion = internStats.Count == 0 ? 0 :
                    Math.Round(internStats.Average(i => i.CompletionRate), 1),
            },
            Interns = internStats,
        });
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        var activePeriod = await _context.InternshipPeriods
            .Include(p => p.Assignments)
            .FirstOrDefaultAsync(p => p.IsActive);

        return Ok(new
        {
            Users = new
            {
                TotalInterns = await _context.Users
                    .Join(_context.UserRoles, u => u.Id, ur => ur.UserId,
                        (u, ur) => new { u, ur })
                    .Join(_context.Roles, x => x.ur.RoleId, r => r.Id,
                        (x, r) => new { x.u, r })
                    .CountAsync(x => x.r.Name == "Intern"),
                TotalMentors = await _context.Users
                    .Join(_context.UserRoles, u => u.Id, ur => ur.UserId,
                        (u, ur) => new { u, ur })
                    .Join(_context.Roles, x => x.ur.RoleId, r => r.Id,
                        (x, r) => new { x.u, r })
                    .CountAsync(x => x.r.Name == "Mentor"),
            },
            ActivePeriod = activePeriod == null ? null : new
            {
                activePeriod.Id,
                activePeriod.Name,
                activePeriod.StartDate,
                activePeriod.EndDate,
                TotalInterns = activePeriod.Assignments.Count,
            },
            Tasks = new
            {
                Total = await _context.TaskItems.CountAsync(),
                Done = await _context.TaskItems.CountAsync(t => t.Status == "DONE"),
                Overdue = await _context.TaskItems.CountAsync(t =>
                    t.Deadline < DateTime.UtcNow && t.Status != "DONE"),
            },
            Recruitment = new
            {
                OpenJDs = await _context.JobDescriptions.CountAsync(j => j.Status == "OPEN"),
                PendingCVs = await _context.Applications.CountAsync(a => a.Status == "PENDING"),
                ScreenedCVs = await _context.AIScreeningResults.CountAsync(),
            },
            LMS = new
            {
                TotalCourses = await _context.Courses.CountAsync(),
                PublishedCourses = await _context.Courses.CountAsync(c => c.IsPublished),
                TotalCertificates = await _context.Certificates.CountAsync(),
            },
        });
    }
}