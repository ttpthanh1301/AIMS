using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using AIMS.BackendServer.Extensions;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CertificatesController : ControllerBase
{
    private readonly AimsDbContext _context;

    public CertificatesController(AimsDbContext context)
        => _context = context;

    // ─────────────────────────────────────────────────────────
    // POST /api/certificates/generate/{attemptId}
    // Tự động sinh Certificate nếu IsPassed = true
    // ─────────────────────────────────────────────────────────
    [HttpPost("generate/{attemptId}")]
    [Authorize(Roles = "Intern")]
    public async Task<IActionResult> Generate(int attemptId)
    {
            var userId = User.GetUserId();

        var attempt = await _context.UserQuizAttempts
            .Include(a => a.QuizBank)
                .ThenInclude(q => q.Course)
            .FirstOrDefaultAsync(a => a.Id == attemptId
                                   && a.InternUserId == userId);

        if (attempt == null)
            return NotFound(new { message = "Attempt không tồn tại." });

        if (attempt.IsPassed != true)
            return BadRequest(new
            {
                message = $"Bạn chưa vượt qua bài kiểm tra. Điểm {attempt.TotalScore} chưa đủ."
            });

        // Kiểm tra đã có certificate chưa
        var existing = await _context.Certificates
            .FirstOrDefaultAsync(c => c.AttemptId == attemptId);

        if (existing != null)
            return Ok(new
            {
                message = "Certificate đã được cấp trước đó.",
                certificateCode = existing.CertificateCode,
                issuedDate = existing.IssuedDate,
                certificateUrl = existing.CertificateUrl,
            });

        // Sinh certificate mới
        var certCode = $"AIMS-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        var certificate = new Certificate
        {
            InternUserId = userId,
            CourseId = attempt.QuizBank.CourseId,
            AttemptId = attemptId,
            CertificateCode = certCode,
            IssuedDate = DateTime.UtcNow,
            CertificateUrl = $"/certificates/{certCode}",
        };

        _context.Certificates.Add(certificate);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "🎓 Chứng chỉ đã được cấp thành công!",
            certificateCode = certificate.CertificateCode,
            courseTitle = attempt.QuizBank.Course.Title,
            issuedDate = certificate.IssuedDate,
            certificateUrl = certificate.CertificateUrl,
        });
    }

    // GET /api/certificates/my
    // Intern xem tất cả chứng chỉ của mình
    [HttpGet("my")]
    [Authorize(Roles = "Intern")]
    public async Task<IActionResult> GetMyCertificates()
    {
            var userId = User.GetUserId();

        var certs = await _context.Certificates
            .Include(c => c.Course)
            .Include(c => c.Attempt)
                .ThenInclude(a => a.QuizBank)
            .Where(c => c.InternUserId == userId)
            .OrderByDescending(c => c.IssuedDate)
            .Select(c => new
            {
                c.Id,
                c.CertificateCode,
                CourseTitle = c.Course.Title,
                QuizTitle = c.Attempt.QuizBank.Title,
                Score = c.Attempt.TotalScore,
                c.IssuedDate,
                c.CertificateUrl,
            })
            .ToListAsync();

        return Ok(certs);
    }

    // GET /api/certificates/{code}/verify
    // Xác minh chứng chỉ (public — không cần login)
    [HttpGet("{code}/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> Verify(string code)
    {
        var cert = await _context.Certificates
            .Include(c => c.Course)
            .Include(c => c.InternUser)
            .Include(c => c.Attempt)
                .ThenInclude(a => a.QuizBank)
            .FirstOrDefaultAsync(c => c.CertificateCode == code);

        if (cert == null)
            return NotFound(new { valid = false, message = "Chứng chỉ không hợp lệ." });

        return Ok(new
        {
            valid = true,
            certificateCode = cert.CertificateCode,
            holderName = $"{cert.InternUser.FirstName} {cert.InternUser.LastName}",
            courseTitle = cert.Course.Title,
            quizTitle = cert.Attempt.QuizBank.Title,
            score = cert.Attempt.TotalScore,
            issuedDate = cert.IssuedDate,
        });
    }

    // GET /api/certificates  — Admin/Mentor xem tất cả
    [HttpGet]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? courseId = null,
        [FromQuery] string? userId = null)
    {
        var query = _context.Certificates
            .Include(c => c.Course)
            .Include(c => c.InternUser)
            .Include(c => c.Attempt)
            .AsQueryable();

        if (courseId.HasValue)
            query = query.Where(c => c.CourseId == courseId.Value);

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(c => c.InternUserId == userId);

        var result = await query
            .OrderByDescending(c => c.IssuedDate)
            .Select(c => new
            {
                c.Id,
                c.CertificateCode,
                HolderName = c.InternUser.FirstName + " " + c.InternUser.LastName,
                CourseTitle = c.Course.Title,
                Score = c.Attempt.TotalScore,
                c.IssuedDate,
                c.CertificateUrl,
            })
            .ToListAsync();

        return Ok(result);
    }
}