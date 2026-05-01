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
public class TasksController : ControllerBase
{
    private readonly AimsDbContext _context;

    public TasksController(AimsDbContext context)
        => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? assignmentId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null)
    {
        var userId = User.GetUserId();  // ⭐

        var query = _context.TaskItems
            .Include(t => t.Assignment)
                .ThenInclude(a => a.InternUser)
            .Include(t => t.CreatedByUser)
            .AsQueryable();

        if (User.IsInRole("Intern"))
            query = query.Where(t => t.Assignment.InternUserId == userId);

        if (User.IsInRole("Mentor"))
            query = query.Where(t => t.Assignment.MentorUserId == userId);

        if (assignmentId.HasValue)
            query = query.Where(t => t.AssignmentId == assignmentId.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status.ToUpper());

        if (!string.IsNullOrEmpty(priority))
            query = query.Where(t => t.Priority == priority.ToUpper());

        var now = DateTime.UtcNow;

        var result = await query
            .OrderBy(t => t.Deadline)
            .Select(t => new TaskVm
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                AssignmentId = t.AssignmentId,
                InternName = t.Assignment.InternUser.FirstName + " " +
                                 t.Assignment.InternUser.LastName,
                Priority = t.Priority,
                Status = t.Status,
                Deadline = t.Deadline,
                EstimatedHours = t.EstimatedHours,
                CreateDate = t.CreateDate,
                CreatedBy = t.CreatedByUser.FirstName + " " +
                                 t.CreatedByUser.LastName,
                IsOverdue = t.Deadline < now && t.Status != "DONE",
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = User.GetUserId();

        var task = await _context.TaskItems
            .Include(t => t.Assignment)
                .ThenInclude(a => a.InternUser)
            .Include(t => t.Assignment)
                .ThenInclude(a => a.MentorUser)
            .Include(t => t.CreatedByUser)
            .Include(t => t.Activities)
                .ThenInclude(a => a.ChangedByUser)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
            return NotFound(new { message = $"Task #{id} không tồn tại." });

        if (User.IsInRole("Intern") && task.Assignment.InternUserId != userId)
            return Forbid();

        if (User.IsInRole("Mentor") && task.Assignment.MentorUserId != userId)
            return Forbid();

        var result = new TaskDetailVm
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            AssignmentId = task.AssignmentId,
            InternName = $"{task.Assignment.InternUser.FirstName} {task.Assignment.InternUser.LastName}",
            Priority = task.Priority,
            Status = task.Status,
            Deadline = task.Deadline,
            EstimatedHours = task.EstimatedHours,
            CreateDate = task.CreateDate,
            CreatedBy = $"{task.CreatedByUser.FirstName} {task.CreatedByUser.LastName}",
            IsOverdue = task.Deadline < DateTime.UtcNow && task.Status != "DONE",
            Activities = task.Activities
                .OrderByDescending(a => a.ChangedAt)
                .Select(a => new TaskActivityVm
                {
                    FromStatus = a.FromStatus,
                    ToStatus = a.ToStatus,
                    Note = a.Note,
                    ChangedBy = $"{a.ChangedByUser.FirstName} {a.ChangedByUser.LastName}",
                    ChangedAt = a.ChangedAt,
                })
                .ToList(),
        };

        return Ok(result);
    }

    [HttpGet("{id}/activities")]
    public async Task<IActionResult> GetActivities(int id)
    {
        var userId = User.GetUserId();

        var task = await _context.TaskItems
            .Include(t => t.Assignment)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
            return NotFound(new { message = $"Task #{id} không tồn tại." });

        if (User.IsInRole("Intern") && task.Assignment.InternUserId != userId)
            return Forbid();

        if (User.IsInRole("Mentor") && task.Assignment.MentorUserId != userId)
            return Forbid();

        var activities = await _context.TaskActivities
            .Include(a => a.ChangedByUser)
            .Where(a => a.TaskId == id)
            .OrderByDescending(a => a.ChangedAt)
            .Select(a => new TaskActivityVm
            {
                FromStatus = a.FromStatus,
                ToStatus = a.ToStatus,
                Note = a.Note,
                ChangedBy = a.ChangedByUser.FirstName + " " + a.ChangedByUser.LastName,
                ChangedAt = a.ChangedAt,
            })
            .ToListAsync();

        return Ok(activities);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request)
    {
        var userId = User.GetUserId();  // ⭐

        var assignment = await _context.InternAssignments
            .FindAsync(request.AssignmentId);
        if (assignment == null)
            return NotFound(new { message = "Assignment không tồn tại." });

        if (User.IsInRole("Mentor") && assignment.MentorUserId != userId)
            return Forbid();

        var task = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            AssignmentId = request.AssignmentId,
            Priority = request.Priority.ToUpper(),
            Status = "TODO",
            Deadline = request.Deadline,
            EstimatedHours = request.EstimatedHours,
            CreateDate = DateTime.UtcNow,
            CreatedByUserId = userId,
        };

        _context.TaskItems.Add(task);
        await _context.SaveChangesAsync();

        _context.TaskActivities.Add(new TaskActivity
        {
            TaskId = task.Id,
            FromStatus = null,
            ToStatus = "TODO",
            Note = "Task được tạo",
            ChangedByUserId = userId,
            ChangedAt = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById),
            new { id = task.Id },
            new { task.Id, task.Title, task.Status });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> Update(
        int id, [FromBody] UpdateTaskRequest request)
    {
        var userId = User.GetUserId();  // ⭐

        var task = await _context.TaskItems
            .Include(t => t.Assignment)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
            return NotFound(new { message = $"Task #{id} không tồn tại." });

        if (User.IsInRole("Mentor") && task.Assignment.MentorUserId != userId)
            return Forbid();

        task.Title = request.Title;
        task.Description = request.Description;
        task.Priority = request.Priority.ToUpper();
        task.Deadline = request.Deadline;
        task.EstimatedHours = request.EstimatedHours;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật task thành công." });
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id, [FromBody] UpdateTaskStatusRequest request)
    {
        var userId = User.GetUserId();  // ⭐

        var validStatuses = new[] { "TODO", "IN_PROGRESS", "DONE", "OVERDUE" };
        if (!validStatuses.Contains(request.Status.ToUpper()))
            return BadRequest(new
            {
                message = $"Status không hợp lệ. Chọn: {string.Join(", ", validStatuses)}"
            });

        var task = await _context.TaskItems
            .Include(t => t.Assignment)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
            return NotFound(new { message = $"Task #{id} không tồn tại." });

        if (User.IsInRole("Intern") && task.Assignment.InternUserId != userId)
            return Forbid();

        var oldStatus = task.Status;
        task.Status = request.Status.ToUpper();

        _context.TaskActivities.Add(new TaskActivity
        {
            TaskId = task.Id,
            FromStatus = oldStatus,
            ToStatus = task.Status,
            Note = request.Note,
            ChangedByUserId = userId,
            ChangedAt = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = $"Đã chuyển task từ '{oldStatus}' → '{task.Status}'",
            taskId = task.Id,
            fromStatus = oldStatus,
            toStatus = task.Status,
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.GetUserId();

        var task = await _context.TaskItems
            .Include(t => t.Assignment)
            .Include(t => t.Activities)
            .Include(t => t.Timesheets)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
            return NotFound(new { message = $"Task #{id} không tồn tại." });

        if (User.IsInRole("Mentor") && task.Assignment.MentorUserId != userId)
            return Forbid();

        _context.TaskActivities.RemoveRange(task.Activities);
        _context.Timesheets.RemoveRange(task.Timesheets);
        _context.TaskItems.Remove(task);

        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã xóa task." });
    }
}
