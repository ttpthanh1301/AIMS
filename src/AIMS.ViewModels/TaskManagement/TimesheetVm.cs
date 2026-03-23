using System.ComponentModel.DataAnnotations;

namespace AIMS.ViewModels.TaskManagement;

public class TimesheetVm
{
    public int Id { get; set; }
    public string InternUserId { get; set; } = string.Empty;
    public string InternName { get; set; } = string.Empty;
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public decimal HoursWorked { get; set; }
    public string? WorkNote { get; set; }
}

public class CreateTimesheetRequest
{
    [Required]
    public int TaskId { get; set; }

    public DateTime? WorkDate { get; set; }
    // Null = hôm nay

    [Required]
    [Range(0.5, 12, ErrorMessage = "Giờ làm từ 0.5 đến 12 giờ/ngày")]
    public decimal HoursWorked { get; set; }

    [StringLength(500)]
    public string? WorkNote { get; set; }
}
public class TimesheetPageVm
{
    public decimal TotalHours { get; set; }
    public List<TimesheetVm> Items { get; set; } = new();
}

public class TimesheetResultVm
{
    public decimal TotalHours { get; set; }
    public List<TimesheetVm> Items { get; set; } = new();
}