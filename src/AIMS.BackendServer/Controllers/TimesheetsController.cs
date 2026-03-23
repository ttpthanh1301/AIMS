using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using AIMS.BackendServer.Extensions;
using AIMS.ViewModels.TaskManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TimesheetsController : ControllerBase
{
    private readonly AimsDbContext _context;

    public TimesheetsController(AimsDbContext context)
        => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll(
    [FromQuery] string? internId = null,
    [FromQuery] int? taskId = null,
    [FromQuery] DateTime? from = null,
    [FromQuery] DateTime? to = null)
    {
        var userId = User.GetUserId();

        var query = _context.Timesheets
            .Include(t => t.InternUser)
            .Include(t => t.Task)
            .AsQueryable();

        if (User.IsInRole("Intern"))
        {
            // Intern chỉ thấy của mình
            query = query.Where(t => t.InternUserId == userId);
        }
        else if (User.IsInRole("Mentor"))
        {
            // ⭐ Mentor thấy tất cả intern của mình
            var internIds = await _context.InternAssignments
                .Where(a => a.MentorUserId == userId)
                .Select(a => a.InternUserId)
                .ToListAsync();

            query = query.Where(t => internIds.Contains(t.InternUserId));

            // Nếu filter thêm theo internId cụ thể
            if (!string.IsNullOrEmpty(internId))
                query = query.Where(t => t.InternUserId == internId);
        }
        else if (!string.IsNullOrEmpty(internId))
        {
            // Admin filter theo internId nếu có
            query = query.Where(t => t.InternUserId == internId);
        }

        if (taskId.HasValue)
            query = query.Where(t => t.TaskId == taskId.Value);
        if (from.HasValue)
            query = query.Where(t => t.WorkDate >= from.Value);
        if (to.HasValue)
            query = query.Where(t => t.WorkDate <= to.Value);

        var result = await query
            .OrderByDescending(t => t.WorkDate)
            .Select(t => new TimesheetVm
            {
                Id = t.Id,
                InternUserId = t.InternUserId,
                InternName = t.InternUser.FirstName + " " + t.InternUser.LastName,
                TaskId = t.TaskId,
                TaskTitle = t.Task.Title,
                WorkDate = t.WorkDate,
                HoursWorked = t.HoursWorked,
                WorkNote = t.WorkNote,
            })
            .ToListAsync();

        var totalHours = result.Sum(t => t.HoursWorked);
        return Ok(new { totalHours, items = result });
    }
    [HttpPost]
    [Authorize(Roles = "Intern")]
    public async Task<IActionResult> Create(
        [FromBody] CreateTimesheetRequest request)
    {
        var userId = User.GetUserId();  // ⭐
        var workDate = request.WorkDate?.Date ?? DateTime.UtcNow.Date;

        var task = await _context.TaskItems
            .Include(t => t.Assignment)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId);

        if (task == null)
            return NotFound(new { message = "Task không tồn tại." });

        if (task.Assignment.InternUserId != userId)
            return Forbid();

        var hoursToday = await _context.Timesheets
            .Where(t => t.InternUserId == userId &&
                        t.WorkDate.Date == workDate)
            .SumAsync(t => t.HoursWorked);

        if (hoursToday + request.HoursWorked > 12)
            return BadRequest(new
            {
                message = $"Tổng giờ làm ngày {workDate:dd/MM} không vượt quá 12h. " +
                          $"Đã log: {hoursToday}h, thêm: {request.HoursWorked}h"
            });

        var timesheet = new Timesheet
        {
            InternUserId = userId,
            TaskId = request.TaskId,
            WorkDate = workDate,
            HoursWorked = request.HoursWorked,
            WorkNote = request.WorkNote,
        };

        _context.Timesheets.Add(timesheet);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = timesheet.Id,
            workDate = timesheet.WorkDate,
            hoursWorked = timesheet.HoursWorked,
            message = "Log giờ thành công.",
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Intern")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.GetUserId();  // ⭐
        var timesheet = await _context.Timesheets.FindAsync(id);

        if (timesheet == null)
            return NotFound(new { message = "Timesheet không tồn tại." });

        if (timesheet.InternUserId != userId)
            return Forbid();

        if ((DateTime.UtcNow - timesheet.WorkDate).TotalHours > 24)
            return BadRequest(new { message = "Chỉ xóa được timesheet trong vòng 24 giờ." });

        _context.Timesheets.Remove(timesheet);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã xóa timesheet." });
    }
}