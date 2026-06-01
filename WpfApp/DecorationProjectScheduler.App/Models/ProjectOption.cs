namespace DecorationProjectScheduler.App.Models;

public sealed class ProjectOption
{
    public int ProjectId { get; init; }

    public string ProjectCode { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string TagColor { get; init; } = "#DCEBFF";

    public string DisplayName => Name;

    public override string ToString() => DisplayName;
}
