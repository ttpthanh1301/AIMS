namespace AIMS.BackendServer.Data.Entities;

public class InternshipPeriod
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;    // VD: "Kỳ Hè 2025"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<InternAssignment> Assignments { get; set; } = new List<InternAssignment>();
}