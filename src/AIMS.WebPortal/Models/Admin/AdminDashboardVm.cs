using AIMS.ViewModels.Systems;
using AIMS.ViewModels.TaskManagement;

namespace AIMS.WebPortal.Models.Admin;

public class AdminDashboardVm
{
    public AdminUserSummaryVm Users { get; set; } = new();
    public AdminActivePeriodVm? ActivePeriod { get; set; }
    public AdminTaskSummaryVm Tasks { get; set; } = new();
    public AdminRecruitmentSummaryVm Recruitment { get; set; } = new();
    public AdminLmsSummaryVm LMS { get; set; } = new();
}

public class AdminUserSummaryVm
{
    public int TotalInterns { get; set; }
    public int TotalMentors { get; set; }
}

public class AdminActivePeriodVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalInterns { get; set; }
}

public class AdminTaskSummaryVm
{
    public int Total { get; set; }
    public int Done { get; set; }
    public int Overdue { get; set; }
}

public class AdminRecruitmentSummaryVm
{
    public int OpenJDs { get; set; }
    public int PendingCVs { get; set; }
    public int ScreenedCVs { get; set; }
}

public class AdminLmsSummaryVm
{
    public int TotalCourses { get; set; }
    public int PublishedCourses { get; set; }
    public int TotalCertificates { get; set; }
}

public class AdminUserIndexVm
{
    public PaginationResult<UserVm> Result { get; set; } = new();
    public UserFilter Filter { get; set; } = new();
    public List<string> AvailableRoles { get; set; } = new() { "Admin", "HR", "Mentor", "Intern" };
}

public class AdminUserEditVm
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Avatar { get; set; }
    public string? StudentId { get; set; }
    public decimal? GPA { get; set; }
    public bool IsActive { get; set; } = true;
    public string SelectedRole { get; set; } = "Intern";
    public List<string> AvailableRoles { get; set; } = new() { "Admin", "HR", "Mentor", "Intern" };
}

public class AdminAssignmentIndexVm
{
    public List<InternAssignmentVm> Assignments { get; set; } = new();
    public List<UserVm> Interns { get; set; } = new();
    public List<UserVm> Mentors { get; set; } = new();
    public List<InternshipPeriodVm> Periods { get; set; } = new();
    public int? PeriodId { get; set; }
}

public class MentorDetailVm
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = "Mentor";
    public List<InternAssignmentVm> AssignedInterns { get; set; } = new();
}
