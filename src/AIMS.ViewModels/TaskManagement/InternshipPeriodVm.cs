using System.ComponentModel.DataAnnotations;

namespace AIMS.ViewModels.TaskManagement;

public class InternshipPeriodVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public int TotalInterns { get; set; }  // Số intern trong kỳ
}

public class CreateInternshipPeriodRequest
{
    [Required(ErrorMessage = "Tên kỳ thực tập không được để trống")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    // VD: "Kỳ Hè 2025", "Kỳ Thu 2025"

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}

public class UpdateInternshipPeriodRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
}