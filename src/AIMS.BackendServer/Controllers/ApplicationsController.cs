using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using AIMS.BackendServer.Helpers;
using AIMS.BackendServer.Services;
using AIMS.ViewModels.Recruitment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly AimsDbContext _context;
    private readonly IAIScreeningService _aiService;
    private readonly IConfiguration _config;

    public ApplicationsController(
        AimsDbContext context,
        IAIScreeningService aiService,
        IConfiguration config)
    {
        _context = context;
        _aiService = aiService;
        _config = config;
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
            },
            // AI Screening Result
            ScreeningResult = app.AIScreeningResult == null ? null : new
            {
                app.AIScreeningResult.MatchingScore,
                app.AIScreeningResult.Ranking,
                app.AIScreeningResult.KeywordsMatched,
                app.AIScreeningResult.KeywordsMissing,
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
    {
        // ── Validate file ─────────────────────────────────────
        if (cvFile == null)
            return BadRequest(new { message = "Vui lòng upload file CV." });

        if (!FileHelper.IsValidCVFile(cvFile))
            return BadRequest(new
            {
                message = "File không hợp lệ. Chỉ chấp nhận PDF/DOC/DOCX, tối đa 5MB."
            });

        // ── Kiểm tra JD tồn tại và còn mở ───────────────────
        var jd = await _context.JobDescriptions
            .FirstOrDefaultAsync(j => j.Id == jobDescriptionId
                                   && j.Status == "OPEN");
        if (jd == null)
            return BadRequest(new { message = "JD không tồn tại hoặc đã đóng." });

        // ── Lấy userId từ JWT ─────────────────────────────────
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst("sub")?.Value ?? "";

        // ── Kiểm tra đã nộp chưa ─────────────────────────────
        var alreadyApplied = await _context.Applications
            .AnyAsync(a => a.ApplicantUserId == userId
                        && a.JobDescriptionId == jobDescriptionId);
        if (alreadyApplied)
            return BadRequest(new { message = "Bạn đã nộp CV cho vị trí này rồi." });

        // ── Lưu file CV ───────────────────────────────────────
        var uploadPath = _config["FileStorage:CVUploadPath"]
            ?? "wwwroot/uploads/cv";
        var savedFileName = await FileHelper.SaveCVFileAsync(cvFile, uploadPath);
        var baseUrl = _config["FileStorage:BaseUrl"] ?? "";
        var cvFileUrl = $"{baseUrl}/uploads/cv/{savedFileName}";

        // ── Tạo Application ───────────────────────────────────
        var application = new Application
        {
            ApplicantUserId = userId,
            JobDescriptionId = jobDescriptionId,
            CVFileUrl = cvFileUrl,
            CoverLetter = coverLetter,
            Status = "PENDING",
            ApplyDate = DateTime.UtcNow,
        };

        _context.Applications.Add(application);
        await _context.SaveChangesAsync();

        // ── Auto Parse CV sau khi nộp ─────────────────────────
        var filePath = Path.Combine(uploadPath, savedFileName);
        try
        {
            var parsedData = await _aiService.ParseCVAsync(
                filePath, application.Id);
            _context.CVParsedDatas.Add(parsedData);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Parse thất bại không ảnh hưởng đến việc nộp CV
            Console.WriteLine($"⚠️ Parse CV failed: {ex.Message}");
        }

        return CreatedAtAction(nameof(GetById),
            new { id = application.Id },
            new
            {
                application.Id,
                application.Status,
                application.ApplyDate,
                message = "Nộp CV thành công! Hệ thống đang xử lý.",
            });
    }

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
}

// ViewModel phụ
public class UpdateApplicationStatusRequest
{
    public string Status { get; set; } = string.Empty;
}