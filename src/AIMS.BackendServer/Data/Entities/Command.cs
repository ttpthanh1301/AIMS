namespace AIMS.BackendServer.Data.Entities;

public class Command
{
    public string Id { get; set; } = string.Empty;   // VD: "CREATE", "DELETE"
    public string Name { get; set; } = string.Empty;

    public ICollection<CommandInFunction> CommandInFunctions { get; set; } = new List<CommandInFunction>();
}