namespace Zerberuz.Analyzers.Rules;

public sealed class DiagnosticHelpDefinition
{
    public string DiagnosticId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Why { get; set; } = string.Empty;

    public string Trigger { get; set; } = string.Empty;

    public string BadExample { get; set; } = string.Empty;

    public string GoodExample { get; set; } = string.Empty;

    public IList<string> FixSteps { get; set; } = new List<string>();

    public string SuppressionGuidance { get; set; } = string.Empty;

    public IList<string> RelatedDiagnostics { get; set; } = new List<string>();
}
