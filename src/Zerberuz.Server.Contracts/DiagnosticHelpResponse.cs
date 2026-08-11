using Zerberuz.Analyzers.Rules;

namespace Zerberuz.Server.Contracts;

public sealed class DiagnosticHelpResponse
{
    public string Profile { get; set; } = string.Empty;

    public string RulesVersion { get; set; } = string.Empty;

    public DiagnosticHelpDefinition Help { get; set; } = new();

    public string HelpHtml { get; set; } = string.Empty;
}
