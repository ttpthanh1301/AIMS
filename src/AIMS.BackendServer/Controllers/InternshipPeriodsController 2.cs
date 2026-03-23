using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using AIMS.ViewModels.TaskManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class InternshipPeriodsController : ControllerBase
{
    private readonly AimsDbContext _context;

    public InternshipPeriodsController(AimsDbContext context)
        => _context = context;

    // GET /api/internshipperiods
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var periods = await _context.InternshipPeriods
            .Include(p => p.Assignments)
            .OrderByDescending(p => p.StartDate)
            .Select(p => new InternshipPeriodVm
            {
                Id = p.Id,
                Name = p.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IsActive = p.IsActive,
                TotalInterns = p.Assignments.Count,
            })
            .ToListAsync();

        return Ok(periods);
    }

    // ⭐ active TRƯỚC {id} để tránh nhầm route
    // GET /api/internshipperiods/active
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var period = await _context.InternshipPeriods
            .Include(p => p.Assignments)
            .FirstOrDefaultAsync(p => p.IsActive);

        if (period == null)
            return NotFound(new
            {
                message = "Hiện không có kỳ thực tập nào đang hoạt động."
            });

        return Ok(new InternshipPeriodVm
        {
            Id = period.Id,
            Name = period.Name,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            IsActive = period.IsActive,
            TotalInterns = period.Assignments.Count,
        });
    }

    // GET /api/internshipperiods/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var period = await _context.InternshipPeriods
            .Include(p => p.Assignments)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (period == null)
            return NotFound(new { message = $"Kỳ thực tập #{id} không tồn tại." });

        return Ok(new InternshipPeriodVm
        {
            Id = period.Id,
            Name = period.Name,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            IsActive = period.IsActive,
            TotalInterns = period.Assignments.Count,
        });
    }

    // POST /api/internshipperiods
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateInternshipPeriodRequest request)
    {
        // ⭐ Fix 1: Check tên trùng
        if (await _context.InternshipPeriods
                .AnyAsync(p => p.Name == request.Name))
            return BadRequest(new
            {
                message = $"Kỳ thực tập '{request.Name}' đã tồn tại."
            });

        // Check ngày hợp lệ
        if (request.EndDate <= request.StartDate)
            return BadRequest(new
            {
                message = "Ngày kết thúc phải sau ngày bắt đầu."
            });

        var period = new InternshipPeriod
        {
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = false,
        };

        _context.InternshipPeriods.Add(period);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById),
            new { id = period.Id },
            new InternshipPeriodVm
            {
                Id = period.Id,
                Name = period.Name,
                StartDate = period.StartDate,
                EndDate = period.EndDate,
                IsActive = period.IsActive,
            });
    }

    // PUT /api/internshipperiods/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id, [FromBody] UpdateInternshipPeriodRequest request)
    {
        var period = await _context.InternshipPeriods.FindAsync(id);
        if (period == null)
            return NotFound(new { message = $"Kỳ thực tập #{id} không tồn tại." });

        // ⭐ Fix 3: Nếu set active thì deactivate kỳ khác
        if (request.IsActive)
        {
            var others = await _context.InternshipPeriods
                .Where(p => p.Id != id)
                .ToListAsync();
            foreach (var p in others)
                p.IsActive = false;
        }

        period.Name = request.Name;
        period.StartDate = request.StartDate;
        period.EndDate = request.EndDate;
        period.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật thành công." });
    }

    // PUT /api/internshipperiods/{id}/activate
    [HttpPut("{id}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Activate(int id)
    {
        var period = await _context.InternshipPeriods.FindAsync(id);
        if (period == null)
            return NotFound(new { message = $"Kỳ thực tập #{id} không tồn tại." });

        var allPeriods = await _context.InternshipPeriods.ToListAsync();
        foreach (var p in allPeriods)
            p.IsActive = false;

        period.IsActive = true;

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Đã kích hoạt kỳ '{period.Name}'." });
    }

    // PUT /api/internshipperiods/{id}/close
    [HttpPut("{id}/close")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Close(int id)
    {
        var period = await _context.InternshipPeriods.FindAsync(id);
        if (period == null)
            return NotFound(new { message = $"Kỳ thực tập #{id} không tồn tại." });

        // ⭐ Check có intern đang hoạt động không
        var hasActiveInterns = await _context.InternAssignments
            .AnyAsync(a => a.PeriodId == id);

        if (hasActiveInterns)
            return BadRequest(new
            {
                message = "Kỳ này còn intern đang hoạt động. Không thể đóng."
            });

        period.IsActive = false;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Đã đóng kỳ '{period.Name}'." });
    }
}