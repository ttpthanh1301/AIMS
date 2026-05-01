using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using AIMS.BackendServer.Helpers;
using AIMS.BackendServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIMS.ViewModels.Recruitment;
using AIMS.BackendServer.Extensions;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,HR,Mentor")]
public class ScreeningController : ControllerBase
{
    private readonly AimsDbContext _context;
    private readonly IAIScreeningService _aiService;
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _config;
    private readonly ILogger<ScreeningController> _logger;

    public ScreeningController(
        AimsDbContext context,
        IAIScreeningService aiService,
        UserManager<AppUser> userManager,
        IConfiguration config,
        ILogger<ScreeningController> logger)
    {
        _context = context;
        _aiService = aiService;
        _userManager = userManager;
        _config = config;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────
    // POST /api/screening/{applicationId}
    // HR trigger AI chấm điểm 1 ứng viên
    // ─────────────────────────────────────────────────────────
    [HttpPost("{applicationId}")]
    public async Task<IActionResult> ScreenOne(int applicationId)
    {
        var app = await _context.Applications
            .Include(a => a.CVParsedData)
            .Include(a => a.JobDescription)
            .Include(a => a.AIScreeningResult)
            .Include(a => a.ApplicantUser)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (app == null)
            return NotFound(new { message = $"Application #{applicationId} không tồn tại." });

        if (app.CVParsedData == null)
            return BadRequest(new { message = "CV chưa được parse. Vui lòng parse trước." });

        var result = await ProcessScreeningAsync(app);

        await _context.SaveChangesAsync();
        await UpdateRankingAsync(app.JobDescriptionId);

        return Ok(new
        {
            applicationId = applicationId,
            matchingScore = result.MatchingScore,
            keywordsMatched = result.KeywordsMatched,
            keywordsMissing = result.KeywordsMissing,
            processingStatus = result.ProcessingStatus,
            errorMessage = result.ErrorMessage,
            screenedAt = result.ScreenedAt,
        });
    }

    // ─────────────────────────────────────────────────────────
    // POST /api/screening/upload/{jdId}
    // HR upload hàng loạt CV PDF cho 1 JD, hệ thống parse + chấm điểm ngay
    // ─────────────────────────────────────────────────────────
    [HttpPost("upload/{jdId}")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> UploadBatch(int jdId, [FromForm] List<IFormFile> cvFiles)
    {
        var jd = await _context.JobDescriptions
            .FirstOrDefaultAsync(j => j.Id == jdId && j.Status == "OPEN");

        if (jd == null)
            return BadRequest(new { message = "JD không tồn tại hoặc đã đóng." });

        if (cvFiles == null || cvFiles.Count == 0)
            return BadRequest(new { message = "Vui lòng chọn ít nhất một file PDF." });

        var uploadPath = _config["FileStorage:CVUploadPath"] ?? "wwwroot/uploads/cv";
        var baseUrl = _config["FileStorage:BaseUrl"] ?? "";
        var results = new List<object>();
        var processed = 0;
        var failed = 0;

        foreach (var file in cvFiles)
        {
            if (!FileHelper.IsValidCVFile(file))
            {
                failed++;
                results.Add(new
                {
                    fileName = file.FileName,
                    processingStatus = "Failed",
                    errorMessage = FileHelper.InvalidCVFileMessage,
                });
                continue;
            }

            var savedFileName = await FileHelper.SaveCVFileAsync(file, uploadPath);
            var cvFileUrl = $"{baseUrl}/uploads/cv/{savedFileName}";
            var candidate = await CreateCandidateUserAsync(file.FileName, cvFileUrl);
            var application = new Application
            {
                ApplicantUserId = candidate.Id,
                JobDescriptionId = jdId,
                CVFileUrl = cvFileUrl,
                CoverLetter = "Uploaded by HR for AI screening.",
                Status = "PENDING",
                ApplyDate = DateTime.UtcNow,
            };

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            try
            {
                var filePath = Path.Combine(uploadPath, savedFileName);
                var parsedData = await _aiService.ParseCVAsync(filePath, application.Id);
                _context.CVParsedDatas.Add(parsedData);

                ApplyParsedCandidateInfo(candidate, parsedData);

                var screeningResult = await _aiService.ScreenCVAsync(
                    parsedData.RawText ?? parsedData.SkillsExtracted ?? string.Empty,
                    BuildJobDescriptionText(jd),
                    application.Id,
                    candidate.GPA,
                    jd.MinGPA);
                _context.AIScreeningResults.Add(screeningResult);
                application.Status = screeningResult.ProcessingStatus == "Completed"
                    ? "SCREENING"
                    : application.Status;

                await _context.SaveChangesAsync();

                if (screeningResult.ProcessingStatus == "Completed")
                    processed++;
                else
                    failed++;

                results.Add(new
                {
                    fileName = file.FileName,
                    applicationId = application.Id,
                    applicantName = $"{candidate.FirstName} {candidate.LastName}",
                    applicantEmail = candidate.Email,
                    matchingScore = screeningResult.MatchingScore,
                    keywordsMatched = screeningResult.KeywordsMatched,
                    keywordsMissing = screeningResult.KeywordsMissing,
                    processingStatus = screeningResult.ProcessingStatus,
                    errorMessage = screeningResult.ErrorMessage,
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI screening upload failed for file {FileName}.", file.FileName);
                failed++;
                _context.AIScreeningResults.Add(new AIScreeningResult
                {
                    ApplicationId = application.Id,
                    MatchingScore = 0,
                    KeywordsMatched = string.Empty,
                    KeywordsMissing = string.Empty,
                    ProcessingStatus = "Failed",
                    ErrorMessage = ex.Message,
                    ScreenedAt = DateTime.UtcNow,
                });
                await _context.SaveChangesAsync();

                results.Add(new
                {
                    fileName = file.FileName,
                    applicationId = application.Id,
                    processingStatus = "Failed",
                    errorMessage = ex.Message,
                });
            }
        }

        await UpdateRankingAsync(jdId);

        return Ok(new
        {
            jdId,
            processed,
            failed,
            message = $"Đã xử lý {processed} CV, lỗi {failed} CV.",
            results,
        });
    }

    // ─────────────────────────────────────────────────────────
    // POST /api/screening/batch/{jdId}
    // HR trigger AI chấm điểm TẤT CẢ ứng viên của 1 JD
    // ─────────────────────────────────────────────────────────
    [HttpPost("batch/{jdId}")]
    public async Task<IActionResult> ScreenBatch(int jdId)
    {
        var jd = await _context.JobDescriptions
            .FirstOrDefaultAsync(j => j.Id == jdId);

        if (jd == null)
            return NotFound(new { message = $"JD #{jdId} không tồn tại." });

        var applications = await _context.Applications
            .Include(a => a.CVParsedData)
            .Include(a => a.AIScreeningResult)
            .Include(a => a.ApplicantUser)
            .Include(a => a.JobDescription)
            .Where(a => a.JobDescriptionId == jdId
                     && a.CVParsedData != null)
            .ToListAsync();

        if (!applications.Any())
            return BadRequest(new { message = "Không có CV nào để chấm điểm." });

        int processed = 0;

        foreach (var app in applications)
        {
            await ProcessScreeningAsync(app);
            processed++;
        }

        await _context.SaveChangesAsync();

        // Cập nhật ranking theo MatchingScore DESC
        await UpdateRankingAsync(jdId);

        return Ok(new
        {
            jdId = jdId,
            processed = processed,
            message = $"Đã chấm điểm {processed} CV. Ranking đã được cập nhật.",
        });
    }

    // ─────────────────────────────────────────────────────────
    // Private: Cập nhật ranking sau khi chấm điểm batch
    // ─────────────────────────────────────────────────────────
    private async Task UpdateRankingAsync(int jdId)
    {
        var results = await _context.AIScreeningResults
            .Include(r => r.Application)
            .Where(r => r.Application.JobDescriptionId == jdId)
            .Where(r => r.ProcessingStatus == "Completed")
            .OrderByDescending(r => r.MatchingScore)
            .ToListAsync();

        for (int i = 0; i < results.Count; i++)
            results[i].Ranking = i + 1;

        await _context.SaveChangesAsync();
    }
    // ─────────────────────────────────────────────────────────
    // GET /api/screening/ranking/{jdId}
    // Lấy ranking list cho 1 JD — sắp xếp MatchingScore DESC
    // ─────────────────────────────────────────────────────────
    [HttpGet("ranking/{jdId}")]
    public async Task<IActionResult> GetRanking(
        int jdId,
        [FromQuery] int top = 20,    // Top N ứng viên
        [FromQuery] decimal minScore = 0)  // Lọc theo điểm tối thiểu
    {
        var jdExists = await _context.JobDescriptions
            .AnyAsync(j => j.Id == jdId);

        if (!jdExists)
            return NotFound(new { message = $"JD #{jdId} không tồn tại." });

        var results = await _context.AIScreeningResults
            .Include(r => r.Application)
                .ThenInclude(a => a.ApplicantUser)
            .Where(r => r.Application.JobDescriptionId == jdId
                     && r.MatchingScore >= minScore)
            .OrderByDescending(r => r.MatchingScore)
            .Take(top)
            .ToListAsync();

        var ranking = results
            .Select((r, index) => new RankingItemVm
            {
                Rank = r.ProcessingStatus == "Completed" ? r.Ranking ?? index + 1 : 0,
                ApplicationId = r.ApplicationId,
                ApplicantName = r.Application.ApplicantUser.FirstName + " " +
                                  r.Application.ApplicantUser.LastName,
                ApplicantEmail = r.Application.ApplicantUser.Email ?? "",
                CVFileUrl = r.Application.CVFileUrl,
                MatchingScore = r.MatchingScore,
                KeywordsMatched = r.KeywordsMatched ?? "",
                KeywordsMissing = r.KeywordsMissing ?? "",
                ProcessingStatus = r.ProcessingStatus,
                ErrorMessage = r.ErrorMessage,
                Status = r.Application.Status,
                ApplyDate = r.Application.ApplyDate,
                ScreenedAt = r.ScreenedAt,
            })
            .ToList();

        return Ok(new
        {
            JdId = jdId,
            Total = ranking.Count,
            MinScore = minScore,
            Ranking = ranking,
        });
    }

    private async Task<AIScreeningResult> ProcessScreeningAsync(Application app)
    {
        if (app.AIScreeningResult != null)
            _context.AIScreeningResults.Remove(app.AIScreeningResult);

        try
        {
            var cvText = app.CVParsedData?.RawText ?? app.CVParsedData?.SkillsExtracted ?? string.Empty;
            var result = await _aiService.ScreenCVAsync(
                cvText,
                BuildJobDescriptionText(app.JobDescription),
                app.Id,
                app.ApplicantUser.GPA,
                app.JobDescription.MinGPA);

            _context.AIScreeningResults.Add(result);
            if (result.ProcessingStatus == "Completed")
                app.Status = "SCREENING";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI screening failed for application {ApplicationId}.", app.Id);
            var result = new AIScreeningResult
            {
                ApplicationId = app.Id,
                MatchingScore = 0,
                KeywordsMatched = string.Empty,
                KeywordsMissing = string.Empty,
                ProcessingStatus = "Failed",
                ErrorMessage = ex.Message,
                ScreenedAt = DateTime.UtcNow,
            };
            _context.AIScreeningResults.Add(result);
            return result;
        }
    }

    private async Task<AppUser> CreateCandidateUserAsync(string originalFileName, string cvFileUrl)
    {
        var normalizedName = Path.GetFileNameWithoutExtension(originalFileName)
            .Replace('_', ' ')
            .Replace('-', ' ')
            .Trim();
        var id = Guid.NewGuid().ToString();
        var email = $"candidate.{Guid.NewGuid():N}@local.aims";
        var candidate = new AppUser
        {
            Id = id,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Ung vien",
            LastName = string.IsNullOrWhiteSpace(normalizedName) ? "CV" : normalizedName,
            CVFileUrl = cvFileUrl,
            IsActive = true,
            CreateDate = DateTime.UtcNow,
        };

        var result = await _userManager.CreateAsync(candidate, "Candidate@2025");
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        if (await _context.Roles.AnyAsync(r => r.NormalizedName == "INTERN"))
            await _userManager.AddToRoleAsync(candidate, "Intern");

        return candidate;
    }

    private void ApplyParsedCandidateInfo(AppUser candidate, CVParsedData parsedData)
    {
        if (!string.IsNullOrWhiteSpace(parsedData.FullName))
        {
            var names = parsedData.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (names.Length > 1)
            {
                candidate.FirstName = names.Last();
                candidate.LastName = string.Join(' ', names.Take(names.Length - 1));
            }
            else
            {
                candidate.FirstName = parsedData.FullName;
                candidate.LastName = "CV";
            }
        }

        if (!string.IsNullOrWhiteSpace(parsedData.EmailExtracted))
        {
            var exists = _context.Users.Any(u =>
                u.Id != candidate.Id && u.NormalizedEmail == parsedData.EmailExtracted.ToUpperInvariant());
            if (!exists)
            {
                candidate.Email = parsedData.EmailExtracted;
                candidate.UserName = parsedData.EmailExtracted;
                candidate.NormalizedEmail = parsedData.EmailExtracted.ToUpperInvariant();
                candidate.NormalizedUserName = parsedData.EmailExtracted.ToUpperInvariant();
            }
        }

        if (!string.IsNullOrWhiteSpace(parsedData.PhoneExtracted))
            candidate.PhoneNumber = parsedData.PhoneExtracted;

        _context.Users.Update(candidate);
    }

    private static string BuildJobDescriptionText(JobDescription jd)
        => string.Join('\n', new[]
        {
            jd.Title,
            jd.DetailContent,
            jd.RequiredSkills,
            jd.MinGPA.HasValue ? $"Minimum GPA {jd.MinGPA.Value}" : string.Empty,
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
}
