using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using AIMS.BackendServer.Helpers;
using AIMS.BackendServer.Services;
using AIMS.ViewModels.Recruitment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using AIMS.BackendServer.Extensions;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly AimsDbContext _context;
    private readonly IAIScreeningService _aiService;
    private readonly IConfiguration _config;
    private readonly ILogger<ApplicationsController> _logger;

    public ApplicationsController(
        AimsDbContext context,
        IAIScreeningService aiService,
        IConfiguration config,
        ILogger<ApplicationsController> logger)
    {
        _context = context;
        _aiService = aiService;
        _config = config;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────
    // GET /api/applications?jdId=1&status=PENDING
    // HR xem danh sách ứng viên
    // ─────────────────────────────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? jdId = null,
        [FromQuery] string? status = null)
    {
        var query = _context.Applications
            .Include(a => a.ApplicantUser)
            .Include(a => a.JobDescription)
            .Include(a => a.AIScreeningResult)
            .AsQueryable();

        if (jdId.HasValue)
            query = query.Where(a => a.JobDescriptionId == jdId.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status.ToUpper());

        var result = await query
            .OrderByDescending(a => a.ApplyDate)
            .Select(a => new ApplicationVm
            {
                Id = a.Id,
                JobDescriptionId = a.JobDescriptionId,
                JobTitle = a.JobDescription.Title,
                ApplicantName = a.ApplicantUser.FirstName + " " +
                                   a.ApplicantUser.LastName,
                ApplicantEmail = a.ApplicantUser.Email ?? "",
                CVFileUrl = a.CVFileUrl,
                CoverLetter = a.CoverLetter,
                Status = a.Status,
                ApplyDate = a.ApplyDate,
                MatchingScore = a.AIScreeningResult != null
                    ? a.AIScreeningResult.MatchingScore : null,
                Ranking = a.AIScreeningResult != null
                    ? a.AIScreeningResult.Ranking : null,
                ScreeningProcessingStatus = a.AIScreeningResult != null
                    ? a.AIScreeningResult.ProcessingStatus : null,
                ScreeningErrorMessage = a.AIScreeningResult != null
                    ? a.AIScreeningResult.ErrorMessage : null,
            })
            .ToListAsync();

        return Ok(result);
    }

    // ─────────────────────────────────────────────────────────
    // GET /api/applications/{id}
    // ─────────────────────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var app = await _context.Applications
            .Include(a => a.ApplicantUser)
            .Include(a => a.JobDescription)
            .Include(a => a.CVParsedData)
            .Include(a => a.AIScreeningResult)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (app == null)
            return NotFound(new { message = $"Application #{id} không tồn tại." });

        return Ok(new
        {
            Id = app.Id,
            JobDescriptionId = app.JobDescriptionId,
            JobTitle = app.JobDescription.Title,
            ApplicantName = $"{app.ApplicantUser.FirstName} {app.ApplicantUser.LastName}",
            ApplicantEmail = app.ApplicantUser.Email,
            ApplicantGPA = app.ApplicantUser.GPA,
            CVFileUrl = app.CVFileUrl,
            CoverLetter = app.CoverLetter,
            Status = app.Status,
            ApplyDate = app.ApplyDate,
            // CV Parsed Data
            ParsedData = app.CVParsedData == null ? null : new
            {
                app.CVParsedData.FullName,
                app.CVParsedData.EmailExtracted,
                app.CVParsedData.PhoneExtracted,
                app.CVParsedData.SkillsExtracted,
                app.CVParsedData.EducationExtracted,
                app.CVParsedData.ExperienceExtracted,
                app.CVParsedData.RawText,
            },
            // AI Screening Result
            ScreeningResult = app.AIScreeningResult == null ? null : new
            {
                app.AIScreeningResult.MatchingScore,
                app.AIScreeningResult.Ranking,
                app.AIScreeningResult.KeywordsMatched,
                app.AIScreeningResult.KeywordsMissing,
                app.AIScreeningResult.ProcessingStatus,
                app.AIScreeningResult.ErrorMessage,
                app.AIScreeningResult.ScreenedAt,
            },
        });
    }

    // ─────────────────────────────────────────────────────────
    // POST /api/applications
    // Intern nộp CV — multipart/form-data
    // ─────────────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Intern")]
    [RequestSizeLimit(10 * 1024 * 1024)] // Tối đa 10MB
    public async Task<IActionResult> Submit(
        [FromForm] int jobDescriptionId,
        [FromForm] string? coverLetter,
        IFormFile cvFile)
        => BadRequest(new
        {
            message = "Chức năng intern nộp CV hiện đã được tạm khóa để phát triển lại."
        });

    private static string BuildJobDescriptionText(JobDescription jd)
        => string.Join('\n', new[]
        {
            jd.Title,
            jd.DetailContent,
            jd.RequiredSkills,
            jd.MinGPA.HasValue ? $"Minimum GPA {jd.MinGPA.Value}" : string.Empty,
        }.Where(x => !string.IsNullOrWhiteSpace(x)));

    // ─────────────────────────────────────────────────────────
    // PUT /api/applications/{id}/status
    // HR cập nhật trạng thái ứng viên
    // ─────────────────────────────────────────────────────────
    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> UpdateStatus(
        int id, [FromBody] UpdateApplicationStatusRequest request)
    {
        var app = await _context.Applications.FindAsync(id);
        if (app == null)
            return NotFound(new { message = $"Application #{id} không tồn tại." });

        var validStatuses = new[]
            { "PENDING", "SCREENING", "INTERVIEW", "ACCEPTED", "REJECTED" };

        if (!validStatuses.Contains(request.Status.ToUpper()))
            return BadRequest(new { message = "Status không hợp lệ." });

        app.Status = request.Status.ToUpper();
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Đã cập nhật status → {app.Status}" });
    }

    // ─────────────────────────────────────────────────────────
    // DELETE /api/applications/{id}
    // HR xóa hồ sơ ứng viên
    // ─────────────────────────────────────────────────────────
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Delete(int id)
    {
        var app = await _context.Applications
            .Include(a => a.CVParsedData)
            .Include(a => a.AIScreeningResult)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (app == null)
            return NotFound(new { message = $"Application #{id} không tồn tại." });

        // Xóa các dữ liệu liên quan trước (do cascade delete)
        if (app.AIScreeningResult != null)
            _context.AIScreeningResults.Remove(app.AIScreeningResult);

        if (app.CVParsedData != null)
            _context.CVParsedDatas.Remove(app.CVParsedData);

        // Xóa application
        _context.Applications.Remove(app);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Đã xóa hồ sơ ứng viên #{id}." });
    }
}

// ViewModel phụ
public class UpdateApplicationStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
