using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using AIMS.ViewModels.LMS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using AIMS.BackendServer.Extensions;
namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class EnrollmentsController : ControllerBase
{
    private readonly AimsDbContext _context;

    public EnrollmentsController(AimsDbContext context)
        => _context = context;

    // GET /api/enrollments?userId=xxx
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? userId = null)
    {
        // Intern chỉ xem enrollment của chính mình
        var currentUserId = User.GetUserId();

        if (User.IsInRole("Intern"))
            userId = currentUserId;

        var query = _context.Enrollments
            .Include(e => e.InternUser)
            .Include(e => e.Course)
            .AsQueryable();

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(e => e.InternUserId == userId);

        var result = await query
            .OrderByDescending(e => e.EnrollDate)
            .Select(e => new EnrollmentVm
            {
                Id = e.Id,
                InternUserId = e.InternUserId,
                InternName = e.InternUser.FirstName + " " + e.InternUser.LastName,
                CourseId = e.CourseId,
                CourseTitle = e.Course.Title,
                EnrollDate = e.EnrollDate,
                CompletionPercent = e.CompletionPercent,
                CompletedDate = e.CompletedDate,
            })
            .ToListAsync();

        return Ok(result);
    }

    // GET /api/enrollments/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var enrollment = await _context.Enrollments
            .Include(e => e.InternUser)
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enrollment == null)
            return NotFound(new { message = $"Enrollment #{id} không tồn tại." });

        return Ok(new EnrollmentVm
        {
            Id = enrollment.Id,
            InternUserId = enrollment.InternUserId,
            InternName = $"{enrollment.InternUser.FirstName} {enrollment.InternUser.LastName}",
            CourseId = enrollment.CourseId,
            CourseTitle = enrollment.Course.Title,
            EnrollDate = enrollment.EnrollDate,
            CompletionPercent = enrollment.CompletionPercent,
            CompletedDate = enrollment.CompletedDate,
        });
    }

    // POST /api/enrollments  — Intern đăng ký khóa học
    [HttpPost]
    [Authorize(Roles = "Intern")]
    public async Task<IActionResult> Enroll(
        [FromBody] EnrollCourseRequest request)
    {
        var userId = User.GetUserId();

        // Kiểm tra course tồn tại và đã published
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == request.CourseId
                                   && c.IsPublished);
        if (course == null)
            return BadRequest(new { message = "Course không tồn tại hoặc chưa được mở." });

        // Kiểm tra đã đăng ký chưa (Unique constraint)
        var alreadyEnrolled = await _context.Enrollments
            .AnyAsync(e => e.InternUserId == userId
                        && e.CourseId == request.CourseId);
        if (alreadyEnrolled)
            return BadRequest(new { message = "Bạn đã đăng ký khóa học này rồi." });

        var enrollment = new Enrollment
        {
            InternUserId = userId,
            CourseId = request.CourseId,
            EnrollDate = DateTime.UtcNow,
            CompletionPercent = 0,
        };

        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById),
            new { id = enrollment.Id },
            new
            {
                enrollment.Id,
                enrollment.CourseId,
                enrollment.CompletionPercent,
                message = $"Đăng ký khóa học '{course.Title}' thành công!",
            });
    }
}