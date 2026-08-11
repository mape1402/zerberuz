using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Zerberuz.Analyzers.Tests;

public sealed class InMemoryAdditionalText : AdditionalText
{
    private readonly SourceText text;

    public InMemoryAdditionalText(string path, string text)
    {
        Path = path;
        this.text = SourceText.From(text);
    }

    public override string Path { get; }

    public override SourceText GetText(CancellationToken cancellationToken = default)
    {
        return text;
    }
}
