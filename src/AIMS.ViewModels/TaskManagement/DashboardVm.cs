namespace AIMS.ViewModels.TaskManagement;

// ── Mentor Dashboard ──────────────────────────────────────────
public class MentorDashboardVm
{
    public MentorSummaryVm Summary { get; set; } = new();
    public List<InternStatVm> Interns { get; set; } = new();
}

public class MentorSummaryVm
{
    public int TotalInterns { get; set; }
    public int TotalTasks { get; set; }
    public int TotalDone { get; set; }
    public int TotalOverdue { get; set; }
    public double AvgCompletion { get; set; }
}

public class InternStatVm
{
    public string InternId { get; set; } = string.Empty;
    public string InternName { get; set; } = string.Empty;
    public string? InternEmail { get; set; }
    public int TotalTasks { get; set; }
    public int TodoTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int DoneTasks { get; set; }
    public int OverdueTasks { get; set; }
    public double CompletionRate { get; set; }
    public decimal TotalHours { get; set; }
}

// ── Intern Dashboard ──────────────────────────────────────────
public class InternDashboardVm
{
    public InternTaskSummaryVm Tasks { get; set; } = new();
    public InternLmsSummaryVm LMS { get; set; } = new();
    public InternReportSummaryVm DailyReports { get; set; } = new();
    public List<UpcomingTaskVm> UpcomingDeadlines { get; set; } = new();
}

public class InternTaskSummaryVm
{
    public int Total { get; set; }
    public int Todo { get; set; }
    public int InProgress { get; set; }
    public int Done { get; set; }
    public int Overdue { get; set; }
    public double CompletionRate { get; set; }
    public decimal TotalHoursLogged { get; set; }
}

public class InternLmsSummaryVm
{
    public int CoursesEnrolled { get; set; }
    public int CoursesCompleted { get; set; }
    public double AvgProgress { get; set; }
    public int Certificates { get; set; }
}

public class InternReportSummaryVm
{
    public int Last7Days { get; set; }
    public string Streak { get; set; } = string.Empty;
}

public class UpcomingTaskVm
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public int DaysLeft { get; set; }
}