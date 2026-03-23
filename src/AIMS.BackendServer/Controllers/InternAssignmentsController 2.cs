using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using AIMS.ViewModels.TaskManagement;
using AIMS.BackendServer.Extensions;   // ⭐ Thêm using này
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class InternAssignmentsController : ControllerBase
{
    private readonly AimsDbContext _context;

    public InternAssignmentsController(AimsDbContext context)
        => _context = context;

    // GET /api/internassignments?periodId=1
    [HttpGet]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> GetAll([FromQuery] int? periodId = null)
    {
        var query = _context.InternAssignments
            .Include(a => a.InternUser)
            .Include(a => a.MentorUser)
            .Include(a => a.Period)
            .Include(a => a.Tasks)
            .AsQueryable();

        if (periodId.HasValue)
            query = query.Where(a => a.PeriodId == periodId.Value);

        // ⭐ Dùng GetUserId() thay vì FindFirst thủ công
        if (User.IsInRole("Mentor"))
        {
            var mentorId = User.GetUserId();
            query = query.Where(a => a.MentorUserId == mentorId);
        }

        var result = await query
            .OrderByDescending(a => a.AssignedDate)
            .Select(a => new InternAssignmentVm
            {
                Id = a.Id,
                InternUserId = a.InternUserId,
                InternName = a.InternUser.FirstName + " " + a.InternUser.LastName,
                InternEmail = a.InternUser.Email ?? "",
                MentorUserId = a.MentorUserId,
                MentorName = a.MentorUser.FirstName + " " + a.MentorUser.LastName,
                PeriodId = a.PeriodId,
                PeriodName = a.Period.Name,
                AssignedDate = a.AssignedDate,
                TotalTasks = a.Tasks.Count,
                DoneTasks = a.Tasks.Count(t => t.Status == "DONE"),
            })
            .ToListAsync();

        return Ok(result);
    }

    // GET /api/internassignments/my
    [HttpGet("my")]
    [Authorize(Roles = "Intern")]
    public async Task<IActionResult> GetMyAssignment()
    {
        // ⭐ Dùng GetUserId()
        var internId = User.GetUserId();

        var assignment = await _context.InternAssignments
            .Include(a => a.MentorUser)
            .Include(a => a.Period)
            .Include(a => a.Tasks)
            .Where(a => a.InternUserId == internId)
            .OrderByDescending(a => a.AssignedDate)
            .FirstOrDefaultAsync();

        if (assignment == null)
            return NotFound(new { message = "Bạn chưa được phân công Mentor." });

        return Ok(new InternAssignmentVm
        {
            Id = assignment.Id,
            InternUserId = assignment.InternUserId,
            MentorUserId = assignment.MentorUserId,
            MentorName = $"{assignment.MentorUser.FirstName} {assignment.MentorUser.LastName}",
            PeriodId = assignment.PeriodId,
            PeriodName = assignment.Period.Name,
            AssignedDate = assignment.AssignedDate,
            TotalTasks = assignment.Tasks.Count,
            DoneTasks = assignment.Tasks.Count(t => t.Status == "DONE"),
        });
    }

    // POST /api/internassignments
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateInternAssignmentRequest request)
    {
        var period = await _context.InternshipPeriods.FindAsync(request.PeriodId);
        if (period == null)
            return NotFound(new { message = "Kỳ thực tập không tồn tại." });

        var existing = await _context.InternAssignments
            .AnyAsync(a =>
                a.InternUserId == request.InternUserId &&
                a.PeriodId == request.PeriodId);

        if (existing)
            return BadRequest(new
            {
                message = "Intern này đã được phân công Mentor trong kỳ thực tập này."
            });

        var assignment = new InternAssignment
        {
            InternUserId = request.InternUserId,
            MentorUserId = request.MentorUserId,
            PeriodId = request.PeriodId,
            AssignedDate = DateTime.UtcNow,
        };

        _context.InternAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Phân công thành công.", id = assignment.Id });
    }

    // DELETE /api/internassignments/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var assignment = await _context.InternAssignments
            .Include(a => a.Tasks)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assignment == null)
            return NotFound(new { message = "Phân công không tồn tại." });

        if (assignment.Tasks.Any())
            return BadRequest(new
            {
                message = $"Phân công này còn {assignment.Tasks.Count} task. Xóa task trước."
            });

        _context.InternAssignments.Remove(assignment);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã hủy phân công." });
    }
}