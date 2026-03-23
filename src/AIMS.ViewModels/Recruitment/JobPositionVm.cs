using System.ComponentModel.DataAnnotations;

namespace AIMS.ViewModels.Recruitment;

public class JobPositionVm
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreateDate { get; set; }
    public int TotalJDs { get; set; }  // Số JD đang mở
}

public class CreateJobPositionRequest
{
    [Required(ErrorMessage = "Tên vị trí không được để trống")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }
}

public class UpdateJobPositionRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}