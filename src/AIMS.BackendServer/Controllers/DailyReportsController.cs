using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using AIMS.BackendServer.Extensions;
using AIMS.ViewModels.TaskManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DailyReportsController : ControllerBase
{
    private readonly AimsDbContext _context;
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    public DailyReportsController(AimsDbContext context)
        => _context = context;

    private Task<List<string>> GetMentorInternIdsAsync(string mentorId)
        => _context.InternAssignments
            .Where(a => a.MentorUserId == mentorId)
            .Select(a => a.InternUserId)
            .Distinct()
            .ToListAsync();

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }

    private static DateTime GetVietnamToday()
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone).Date;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? internId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var userId = User.GetUserId();  // ⭐

        var query = _context.DailyReports
            .Include(r => r.InternUser)
            .AsQueryable();

        if (User.IsInRole("Intern"))
        {
            query = query.Where(r => r.InternUserId == userId);
        }
        else if (User.IsInRole("Mentor"))
        {
            var mentorInternIds = await GetMentorInternIdsAsync(userId);
            query = query.Where(r => mentorInternIds.Contains(r.InternUserId));

            if (!string.IsNullOrEmpty(internId))
                query = query.Where(r => r.InternUserId == internId);
        }
        else if (!string.IsNullOrEmpty(internId))
        {
            query = query.Where(r => r.InternUserId == internId);
        }

        if (from.HasValue)
            query = query.Where(r => r.ReportDate >= from.Value);

        if (to.HasValue)
            query = query.Where(r => r.ReportDate <= to.Value);

        var result = await query
            .OrderByDescending(r => r.ReportDate)
            .Select(r => new DailyReportVm
            {
                Id = r.Id,
                InternUserId = r.InternUserId,
                InternName = r.InternUser.FirstName + " " + r.InternUser.LastName,
                ReportDate = r.ReportDate,
                Content = r.Content,
                PlannedTomorrow = r.PlannedTomorrow,
                Issues = r.Issues,
                MentorFeedback = r.MentorFeedback,
                HasFeedback = r.MentorFeedback != null,
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = User.GetUserId();

        var report = await _context.DailyReports
            .Include(r => r.InternUser)
            .Include(r => r.ReviewedByMentor)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null)
            return NotFound(new { message = $"Báo cáo #{id} không tồn tại." });

        if (User.IsInRole("Intern") && report.InternUserId != userId)
            return Forbid();

        if (User.IsInRole("Mentor"))
        {
            var canAccess = await _context.InternAssignments
                .AnyAsync(a =>
                    a.MentorUserId == userId &&
                    a.InternUserId == report.InternUserId);

            if (!canAccess)
                return Forbid();
        }

        return Ok(new DailyReportVm
        {
            Id = report.Id,
            InternUserId = report.InternUserId,
            InternName = $"{report.InternUser.FirstName} {report.InternUser.LastName}",
            ReportDate = report.ReportDate,
            Content = report.Content,
            PlannedTomorrow = report.PlannedTomorrow,
            Issues = report.Issues,
            MentorFeedback = report.MentorFeedback,
            HasFeedback = report.MentorFeedback != null,
        });
    }

    [HttpPost]
    [Authorize(Roles = "Intern")]
    public async Task<IActionResult> Create(
        [FromBody] CreateDailyReportRequest request)
    {
        var userId = User.GetUserId();  // ⭐
        var reportDate = request.ReportDate?.Date ?? GetVietnamToday();

        var exists = await _context.DailyReports
            .AnyAsync(r =>
                r.InternUserId == userId &&
                r.ReportDate.Date == reportDate);

        if (exists)
            return BadRequest(new
            {
                message = $"Bạn đã nộp báo cáo ngày {reportDate:dd/MM/yyyy} rồi."
            });

        var report = new DailyReport
        {
            InternUserId = userId,
            ReportDate = reportDate,
            Content = request.Content,
            PlannedTomorrow = request.PlannedTomorrow,
            Issues = request.Issues,
        };

        _context.DailyReports.Add(report);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById),
            new { id = report.Id },
            new { report.Id, reportDate = report.ReportDate });
    }

    [HttpPut("{id}/feedback")]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> Feedback(
        int id, [FromBody] MentorFeedbackRequest request)
    {
        var mentorId = User.GetUserId();  // ⭐

        var report = await _context.DailyReports.FindAsync(id);
        if (report == null)
            return NotFound(new { message = $"Báo cáo #{id} không tồn tại." });

        if (User.IsInRole("Mentor"))
        {
            var canAccess = await _context.InternAssignments
                .AnyAsync(a =>
                    a.MentorUserId == mentorId &&
                    a.InternUserId == report.InternUserId);

            if (!canAccess)
                return Forbid();
        }

        report.MentorFeedback = request.Feedback;
        report.ReviewedByMentorId = mentorId;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã gửi phản hồi thành công." });
    }
}
