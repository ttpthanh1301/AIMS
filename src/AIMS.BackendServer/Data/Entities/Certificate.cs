namespace AIMS.BackendServer.Data.Entities;

public class Certificate
{
    public int Id { get; set; }
    public string InternUserId { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public int AttemptId { get; set; }
    public string CertificateCode { get; set; } = Guid.NewGuid().ToString();
    public DateTime IssuedDate { get; set; } = DateTime.UtcNow;
    public string? CertificateUrl { get; set; }

    public AppUser InternUser { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public UserQuizAttempt Attempt { get; set; } = null!;
}