using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Zerberuz.Analyzers.Rules;

namespace Zerberuz.Server.Profiles;

internal static partial class MarkdownHelpRenderer
{
    public static string Render(DiagnosticHelpDefinition help)
    {
        var markdown = !string.IsNullOrWhiteSpace(help.Markdown)
            ? help.Markdown
            : BuildMarkdownFromStructuredHelp(help);

        return RenderMarkdown(markdown);
    }

    private static string BuildMarkdownFromStructuredHelp(DiagnosticHelpDefinition help)
    {
        using var writer = new StringWriter();
        writer.WriteLine($"# {help.DiagnosticId}: {help.Title}");
        WriteSection(writer, "Summary", help.Summary);
        WriteSection(writer, "Why", help.Why);
        WriteSection(writer, "Trigger", help.Trigger);
        WriteSection(writer, "Bad Example", help.BadExample);
        WriteSection(writer, "Good Example", help.GoodExample);

        if (help.FixSteps.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("## Fix");
            foreach (var step in help.FixSteps)
            {
                writer.WriteLine("- " + step);
            }
        }

        WriteSection(writer, "Suppression", help.SuppressionGuidance);
        return writer.ToString();
    }

    private static string RenderMarkdown(string markdown)
    {
        var html = new StringBuilder();
        var inList = false;
        var inCode = false;

        foreach (var rawLine in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.TrimEnd();
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                CloseList(html, ref inList);
                if (inCode)
                {
                    html.AppendLine("</code></pre>");
                    inCode = false;
                }
                else
                {
                    html.AppendLine("<pre><code>");
                    inCode = true;
                }

                continue;
            }

            if (inCode)
            {
                html.AppendLine(WebUtility.HtmlEncode(line));
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                CloseList(html, ref inList);
                continue;
            }

            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                CloseList(html, ref inList);
                html.Append("<h1>").Append(RenderInline(line[2..].Trim())).AppendLine("</h1>");
                continue;
            }

            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                CloseList(html, ref inList);
                html.Append("<h2>").Append(RenderInline(line[3..].Trim())).AppendLine("</h2>");
                continue;
            }

            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                CloseList(html, ref inList);
                html.Append("<h3>").Append(RenderInline(line[4..].Trim())).AppendLine("</h3>");
                continue;
            }

            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                if (!inList)
                {
                    html.AppendLine("<ul>");
                    inList = true;
                }

                html.Append("<li>").Append(RenderInline(line[2..].Trim())).AppendLine("</li>");
                continue;
            }

            CloseList(html, ref inList);
            html.Append("<p>").Append(RenderInline(line.Trim())).AppendLine("</p>");
        }

        CloseList(html, ref inList);
        if (inCode)
        {
            html.AppendLine("</code></pre>");
        }

        return html.ToString();
    }

    private static string RenderInline(string value)
    {
        var encoded = WebUtility.HtmlEncode(value);
        encoded = InlineCodeRegex().Replace(encoded, "<code>$1</code>");
        encoded = BoldRegex().Replace(encoded, "<strong>$1</strong>");
        encoded = LinkRegex().Replace(encoded, match =>
        {
            var text = match.Groups[1].Value;
            var href = match.Groups[2].Value;
            return href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                href.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    ? $"<a href=\"{href}\">{text}</a>"
                    : text;
        });

        return encoded;
    }

    private static void WriteSection(TextWriter writer, string title, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine("## " + title);
        writer.WriteLine(value);
    }

    private static void CloseList(StringBuilder html, ref bool inList)
    {
        if (!inList)
        {
            return;
        }

        html.AppendLine("</ul>");
        inList = false;
    }

    [GeneratedRegex("`([^`]+)`")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"\*\*([^*]+)\*\*")]
    private static partial Regex BoldRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\(([^)]+)\)")]
    private static partial Regex LinkRegex();
}
