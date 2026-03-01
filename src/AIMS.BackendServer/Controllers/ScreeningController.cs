using AIMS.BackendServer.Data;
using AIMS.BackendServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIMS.ViewModels.Recruitment;
namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,HR")]
public class ScreeningController : ControllerBase
{
    private readonly AimsDbContext _context;
    private readonly IAIScreeningService _aiService;

    public ScreeningController(
        AimsDbContext context,
        IAIScreeningService aiService)
    {
        _context = context;
        _aiService = aiService;
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
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (app == null)
            return NotFound(new { message = $"Application #{applicationId} không tồn tại." });

        if (app.CVParsedData == null)
            return BadRequest(new { message = "CV chưa được parse. Vui lòng parse trước." });

        // Xóa kết quả cũ nếu có
        if (app.AIScreeningResult != null)
            _context.AIScreeningResults.Remove(app.AIScreeningResult);

        // Gọi AI Service — TF-IDF + Cosine Similarity
        var cvText = app.CVParsedData.RawText ?? app.CVParsedData.SkillsExtracted ?? "";
        var requiredSkills = app.JobDescription.RequiredSkills;

        var result = await ((AIScreeningService)_aiService)
            .ScreenCVAsync(cvText, requiredSkills, applicationId);

        _context.AIScreeningResults.Add(result);

        // Cập nhật status ứng viên
        app.Status = "SCREENING";

        await _context.SaveChangesAsync();

        return Ok(new
        {
            applicationId = applicationId,
            matchingScore = result.MatchingScore,
            keywordsMatched = result.KeywordsMatched,
            keywordsMissing = result.KeywordsMissing,
            screenedAt = result.ScreenedAt,
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

        // Lấy tất cả applications chưa được chấm
        var applications = await _context.Applications
            .Include(a => a.CVParsedData)
            .Include(a => a.AIScreeningResult)
            .Where(a => a.JobDescriptionId == jdId
                     && a.CVParsedData != null)
            .ToListAsync();

        if (!applications.Any())
            return BadRequest(new { message = "Không có CV nào để chấm điểm." });

        int processed = 0;

        foreach (var app in applications)
        {
            // Xóa kết quả cũ
            if (app.AIScreeningResult != null)
                _context.AIScreeningResults.Remove(app.AIScreeningResult);

            var cvText = app.CVParsedData!.RawText
                ?? app.CVParsedData.SkillsExtracted ?? "";

            var result = await ((AIScreeningService)_aiService)
                .ScreenCVAsync(cvText, jd.RequiredSkills, app.Id);

            _context.AIScreeningResults.Add(result);
            app.Status = "SCREENING";
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

        var ranking = await _context.AIScreeningResults
            .Include(r => r.Application)
                .ThenInclude(a => a.ApplicantUser)
            .Where(r => r.Application.JobDescriptionId == jdId
                     && r.MatchingScore >= minScore)
            .OrderByDescending(r => r.MatchingScore)
            .Take(top)
            .Select(r => new RankingItemVm
            {
                Rank = r.Ranking ?? 0,
                ApplicationId = r.ApplicationId,
                ApplicantName = r.Application.ApplicantUser.FirstName + " " +
                                  r.Application.ApplicantUser.LastName,
                ApplicantEmail = r.Application.ApplicantUser.Email ?? "",
                CVFileUrl = r.Application.CVFileUrl,
                MatchingScore = r.MatchingScore,
                KeywordsMatched = r.KeywordsMatched ?? "",
                KeywordsMissing = r.KeywordsMissing ?? "",
                Status = r.Application.Status,
                ApplyDate = r.Application.ApplyDate,
                ScreenedAt = r.ScreenedAt,
            })
            .ToListAsync();

        return Ok(new
        {
            JdId = jdId,
            Total = ranking.Count,
            MinScore = minScore,
            Ranking = ranking,
        });
    }
}