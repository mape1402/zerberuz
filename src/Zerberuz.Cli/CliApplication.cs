using Zerberuz.Analyzers.Configuration;
using Zerberuz.Analyzers.Rules;

namespace Zerberuz.Cli;

public sealed class CliApplication
{
    public int Run(string[] args)
    {
        return Run(
            args,
            Console.Out,
            Console.Error,
            File.Exists,
            File.ReadAllText);
    }

    public int Run(
        string[] args,
        TextWriter output,
        TextWriter error,
        Func<string, bool> fileExists,
        Func<string, string> readAllText,
        Action<string, string>? writeAllText = null)
    {
        if (args.Length == 0)
        {
            WriteUsage(output);
            return 0;
        }

        return args[0] switch
        {
            "init" => Init(args.Skip(1).ToArray(), output, error, fileExists, writeAllText ?? File.WriteAllText),
            "explain" => Explain(args.Skip(1).ToArray(), output, error, fileExists, readAllText),
            "--help" or "-h" => WriteUsageAndReturn(output),
            _ => WriteUnknownCommand(args[0], error)
        };
    }

    private static int Init(
        string[] args,
        TextWriter output,
        TextWriter error,
        Func<string, bool> fileExists,
        Action<string, string> writeAllText)
    {
        var configPath = ResolveOption(args, "--config-path") ?? "zerberuz.json";
        var profile = ResolveOption(args, "--profile") ?? "default";

        if (fileExists(configPath))
        {
            error.WriteLine($"Configuration already exists: {configPath}");
            return 5;
        }

        writeAllText(configPath, $$"""
        {
          "profile": "{{profile}}",
          "rulesVersion": "latest-compatible",
          "mode": "latest-compatible",
          "rulesEndpoint": "https://rules.zerberuz.dev",
          "cachePath": ".zerberuz/rules-cache.json"
        }
        """);

        output.WriteLine($"Created {configPath}");
        return 0;
    }

    private static int Explain(
        string[] args,
        TextWriter output,
        TextWriter error,
        Func<string, bool> fileExists,
        Func<string, string> readAllText)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            error.WriteLine("Diagnostic id is required.");
            error.WriteLine("Usage: zerberuz explain ZBZ001 [--cache-path .zerberuz/rules-cache.json]");
            return 2;
        }

        var diagnosticId = args[0];
        var cachePath = ResolveOption(args, "--cache-path") ?? ".zerberuz/rules-cache.json";

        if (!fileExists(cachePath))
        {
            error.WriteLine($"Rule cache was not found: {cachePath}");
            return 3;
        }

        var ruleSet = new RuleSetCacheLoader().Load(readAllText(cachePath));
        var help = ruleSet?.Help.FirstOrDefault(candidate =>
            string.Equals(candidate.DiagnosticId, diagnosticId, StringComparison.Ordinal));

        if (help is null)
        {
            error.WriteLine($"No cached help was found for diagnostic '{diagnosticId}'.");
            return 4;
        }

        WriteHelp(help, output);
        return 0;
    }

    private static string? ResolveOption(string[] args, string name)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.Ordinal))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static void WriteHelp(DiagnosticHelpDefinition help, TextWriter output)
    {
        output.WriteLine($"{help.DiagnosticId}: {help.Title}");
        WriteSection(output, "Summary", help.Summary);
        WriteSection(output, "Why", help.Why);
        WriteSection(output, "Trigger", help.Trigger);
        WriteSection(output, "Bad Example", help.BadExample);
        WriteSection(output, "Good Example", help.GoodExample);

        if (help.FixSteps.Count > 0)
        {
            output.WriteLine();
            output.WriteLine("Fix");
            for (var index = 0; index < help.FixSteps.Count; index++)
            {
                output.WriteLine($"{index + 1}. {help.FixSteps[index]}");
            }
        }

        WriteSection(output, "Suppression", help.SuppressionGuidance);

        if (help.RelatedDiagnostics.Count > 0)
        {
            output.WriteLine();
            output.WriteLine("Related");
            foreach (var related in help.RelatedDiagnostics)
            {
                output.WriteLine($"- {related}");
            }
        }
    }

    private static void WriteSection(TextWriter output, string title, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        output.WriteLine();
        output.WriteLine(title);
        output.WriteLine(value);
    }

    private static int WriteUsageAndReturn(TextWriter output)
    {
        WriteUsage(output);
        return 0;
    }

    private static void WriteUsage(TextWriter output)
    {
        output.WriteLine("Zerberuz CLI");
        output.WriteLine();
        output.WriteLine("Usage:");
        output.WriteLine("  zerberuz init [--profile default] [--config-path zerberuz.json]");
        output.WriteLine("  zerberuz explain ZBZ001 [--cache-path .zerberuz/rules-cache.json]");
    }

    private static int WriteUnknownCommand(string command, TextWriter error)
    {
        error.WriteLine($"Unknown command: {command}");
        return 1;
    }
}
