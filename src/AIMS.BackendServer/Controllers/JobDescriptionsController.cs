using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using AIMS.ViewModels.Recruitment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class JobDescriptionsController : ControllerBase
{
    private readonly AimsDbContext _context;

    public JobDescriptionsController(AimsDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // GET: api/jobdescriptions?status=OPEN&positionId=1
    // =========================================================
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobDescriptionVm>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] int? positionId)
    {
        var query = _context.JobDescriptions
            .AsNoTracking()
            .Include(j => j.JobPosition)
            .Include(j => j.CreatedByUser)
            .Include(j => j.Applications)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(j => j.Status == status.ToUpper());

        if (positionId.HasValue)
            query = query.Where(j => j.JobPositionId == positionId.Value);

        var data = await query
            .OrderByDescending(j => j.CreateDate)
            .Select(j => new JobDescriptionVm
            {
                Id = j.Id,
                JobPositionId = j.JobPositionId,
                JobPositionTitle = j.JobPosition.Title,
                Title = j.Title,
                DetailContent = j.DetailContent,
                RequiredSkills = j.RequiredSkills,
                MinGPA = j.MinGPA,
                Status = j.Status,
                CreateDate = j.CreateDate,
                DeadlineDate = j.DeadlineDate,
                CreatedByUser = j.CreatedByUser.FirstName + " " + j.CreatedByUser.LastName,
                TotalApplications = j.Applications.Count
            })
            .ToListAsync();

        return Ok(data);
    }

    // =========================================================
    // GET: api/jobdescriptions/{id}
    // =========================================================
    [HttpGet("{id}", Name = "GetJobDescriptionById")]
    public async Task<ActionResult<JobDescriptionVm>> GetById(int id)
    {
        var jd = await _context.JobDescriptions
            .AsNoTracking()
            .Include(j => j.JobPosition)
            .Include(j => j.CreatedByUser)
            .Include(j => j.Applications)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (jd == null)
            return NotFound(new { message = $"JD #{id} không tồn tại." });

        return Ok(new JobDescriptionVm
        {
            Id = jd.Id,
            JobPositionId = jd.JobPositionId,
            JobPositionTitle = jd.JobPosition.Title,
            Title = jd.Title,
            DetailContent = jd.DetailContent,
            RequiredSkills = jd.RequiredSkills,
            MinGPA = jd.MinGPA,
            Status = jd.Status,
            CreateDate = jd.CreateDate,
            DeadlineDate = jd.DeadlineDate,
            CreatedByUser = jd.CreatedByUser.FirstName + " " + jd.CreatedByUser.LastName,
            TotalApplications = jd.Applications.Count
        });
    }

    // =========================================================
    // POST: api/jobdescriptions
    // =========================================================
    [HttpPost]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Create([FromBody] CreateJobDescriptionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var positionExists = await _context.JobPositions
            .AnyAsync(p => p.Id == request.JobPositionId && p.IsActive);

        if (!positionExists)
            return BadRequest(new { message = "JobPosition không tồn tại hoặc đã bị đóng." });

        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var entity = new JobDescription
        {
            JobPositionId = request.JobPositionId,
            Title = request.Title,
            DetailContent = request.DetailContent,
            RequiredSkills = request.RequiredSkills,
            MinGPA = request.MinGPA,
            DeadlineDate = request.DeadlineDate,
            Status = "OPEN",
            CreateDate = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        _context.JobDescriptions.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtRoute(
            "GetJobDescriptionById",
            new { id = entity.Id },
            new { entity.Id, entity.Title, entity.Status }
        );
    }

    // =========================================================
    // PUT: api/jobdescriptions/{id}
    // =========================================================
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateJobDescriptionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var jd = await _context.JobDescriptions.FindAsync(id);
        if (jd == null)
            return NotFound(new { message = $"JD #{id} không tồn tại." });

        jd.Title = request.Title;
        jd.DetailContent = request.DetailContent;
        jd.RequiredSkills = request.RequiredSkills;
        jd.MinGPA = request.MinGPA;
        jd.DeadlineDate = request.DeadlineDate;
        jd.Status = request.Status.ToUpper();

        await _context.SaveChangesAsync();

        return Ok(new { message = "Cập nhật JD thành công." });
    }

    // =========================================================
    // DELETE: api/jobdescriptions/{id}
    // =========================================================
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Delete(int id)
    {
        var jd = await _context.JobDescriptions
            .Include(j => j.Applications)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (jd == null)
            return NotFound(new { message = $"JD #{id} không tồn tại." });

        if (jd.Applications.Any())
            return BadRequest(new
            {
                message = $"JD này có {jd.Applications.Count} ứng viên đã nộp. Không thể xóa."
            });

        _context.JobDescriptions.Remove(jd);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã xóa JD." });
    }
}