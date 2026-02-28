namespace AIMS.BackendServer.Data.Entities;

public class CommandInFunction
{
    public string CommandId { get; set; } = string.Empty;
    public string FunctionId { get; set; } = string.Empty;

    public Command Command { get; set; } = null!;
    public Function Function { get; set; } = null!;
}