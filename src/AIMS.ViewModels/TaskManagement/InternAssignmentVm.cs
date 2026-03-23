using System.ComponentModel.DataAnnotations;

namespace AIMS.ViewModels.TaskManagement;

public class InternAssignmentVm
{
    public int Id { get; set; }
    public string InternUserId { get; set; } = string.Empty;
    public string InternName { get; set; } = string.Empty;
    public string InternEmail { get; set; } = string.Empty;
    public string MentorUserId { get; set; } = string.Empty;
    public string MentorName { get; set; } = string.Empty;
    public int PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public DateTime AssignedDate { get; set; }
    public int TotalTasks { get; set; }
    public int DoneTasks { get; set; }
}

public class CreateInternAssignmentRequest
{
    [Required]
    public string InternUserId { get; set; } = string.Empty;

    [Required]
    public string MentorUserId { get; set; } = string.Empty;

    [Required]
    public int PeriodId { get; set; }
}