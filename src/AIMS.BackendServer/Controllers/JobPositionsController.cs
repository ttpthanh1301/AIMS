using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using AIMS.ViewModels.Recruitment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class JobPositionsController : ControllerBase
{
    private readonly AimsDbContext _context;

    public JobPositionsController(AimsDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // GET: api/jobpositions
    // =========================================================
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobPositionVm>>> GetAll()
    {
        var data = await _context.JobPositions
            .AsNoTracking()
            .OrderByDescending(p => p.CreateDate)
            .Select(p => new JobPositionVm
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                IsActive = p.IsActive,
                CreateDate = p.CreateDate,
                TotalJDs = p.JobDescriptions.Count(j => j.Status == "OPEN")
            })
            .ToListAsync();

        return Ok(data);
    }

    // =========================================================
    // GET: api/jobpositions/{id}
    // =========================================================
    [HttpGet("{id}", Name = "GetJobPositionById")]
    public async Task<ActionResult<JobPositionVm>> GetById(int id)
    {
        var position = await _context.JobPositions
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new JobPositionVm
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                IsActive = p.IsActive,
                CreateDate = p.CreateDate,
                TotalJDs = p.JobDescriptions.Count(j => j.Status == "OPEN")
            })
            .FirstOrDefaultAsync();

        if (position == null)
            return NotFound(new { message = $"JobPosition #{id} không tồn tại." });

        return Ok(position);
    }

    // =========================================================
    // POST: api/jobpositions
    // =========================================================
    [HttpPost]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Create([FromBody] CreateJobPositionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var entity = new JobPosition
        {
            Title = request.Title,
            Description = request.Description,
            IsActive = true,
            CreateDate = DateTime.UtcNow
        };

        _context.JobPositions.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtRoute(
            "GetJobPositionById",
            new { id = entity.Id },
            new JobPositionVm
            {
                Id = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                IsActive = entity.IsActive,
                CreateDate = entity.CreateDate,
                TotalJDs = 0
            }
        );
    }

    // =========================================================
    // PUT: api/jobpositions/{id}
    // =========================================================
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateJobPositionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var entity = await _context.JobPositions.FindAsync(id);
        if (entity == null)
            return NotFound(new { message = $"JobPosition #{id} không tồn tại." });

        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Cập nhật thành công." });
    }

    // =========================================================
    // DELETE: api/jobpositions/{id}
    // =========================================================
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.JobPositions
            .Include(p => p.JobDescriptions)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (entity == null)
            return NotFound(new { message = $"JobPosition #{id} không tồn tại." });

        if (entity.JobDescriptions.Any())
            return BadRequest(new
            {
                message = $"Vị trí này còn {entity.JobDescriptions.Count} JD. Xóa JD trước."
            });

        _context.JobPositions.Remove(entity);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã xóa JobPosition." });
    }
}