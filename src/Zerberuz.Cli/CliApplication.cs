using Zerberuz.Analyzers.Configuration;
using Zerberuz.Analyzers.Rules;
using System.Text.Json;

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
            "sync-rules" => SyncRules(args.Skip(1).ToArray(), output, error),
            "doctor" => Doctor(args.Skip(1).ToArray(), output, error),
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
        var team = ResolveOption(args, "--team") ?? "default";

        if (fileExists(configPath))
        {
            error.WriteLine($"Configuration already exists: {configPath}");
            return 5;
        }

        writeAllText(configPath, $$"""
        {
          "team": "{{team}}",
          "profile": "{{profile}}",
          "rulesVersion": "latest-compatible",
          "mode": "latest-compatible",
          "rulesEndpoint": "https://rules.zerberuz.dev",
          "cacheRoot": null
        }
        """);

        output.WriteLine($"Created {configPath}");
        return 0;
    }

    private static int SyncRules(string[] args, TextWriter output, TextWriter error)
    {
        var source = ResolveOption(args, "--source");
        if (string.IsNullOrWhiteSpace(source))
        {
            error.WriteLine("Rule source is required.");
            error.WriteLine("Usage: zerberuz sync-rules --source <file-or-url> [--config-path zerberuz.json] [--cache-root <path>]");
            return 6;
        }

        var configPath = ResolveOption(args, "--config-path") ?? "zerberuz.json";
        if (!File.Exists(configPath))
        {
            error.WriteLine($"Configuration was not found: {configPath}");
            return 7;
        }

        var configuration = ZerberuzProjectConfiguration.Load(File.ReadAllText(configPath));
        var payload = ReadSource(source);
        var ruleSet = new RuleSetCacheLoader().Load(payload);
        var validation = new RuleSetValidator().Validate(ruleSet);
        if (!validation.IsValid)
        {
            WriteValidationErrors(validation, error);
            return 8;
        }

        var effectiveConfiguration = new ZerberuzProjectConfiguration
        {
            Team = configuration.Team,
            Profile = configuration.Profile,
            RulesVersion = ruleSet!.RulesVersion,
            Mode = configuration.Mode,
            RulesEndpoint = configuration.RulesEndpoint,
            CacheRoot = configuration.CacheRoot
        };

        var paths = new SharedCachePathResolver().Resolve(
            effectiveConfiguration,
            ResolveOption(args, "--cache-root"));

        AtomicWriteValidatedRuleSet(paths.RulesCachePath, payload);
        ExportHelpMarkdown(paths.HelpDirectory, ruleSet!.Help);
        AtomicWriteText(
            paths.LatestCompatiblePointerPath,
            JsonSerializer.Serialize(
                new
                {
                    paths.Team,
                    paths.Profile,
                    paths.RulesVersion,
                    paths.RulesCachePath
                },
                new JsonSerializerOptions { WriteIndented = true }));

        output.WriteLine($"Synced {paths.Team}/{paths.Profile}@{paths.RulesVersion}");
        output.WriteLine($"Cache: {paths.RulesCachePath}");
        return 0;
    }

    private static int Doctor(string[] args, TextWriter output, TextWriter error)
    {
        var configPath = ResolveOption(args, "--config-path") ?? "zerberuz.json";
        if (!File.Exists(configPath))
        {
            error.WriteLine($"Configuration was not found: {configPath}");
            return 7;
        }

        var configuration = ZerberuzProjectConfiguration.Load(File.ReadAllText(configPath));
        var paths = new SharedCachePathResolver().Resolve(
            configuration,
            ResolveOption(args, "--cache-root"));

        output.WriteLine("Zerberuz doctor");
        output.WriteLine($"Config: {configPath}");
        output.WriteLine($"Team: {paths.Team}");
        output.WriteLine($"Profile: {paths.Profile}");
        output.WriteLine($"Requested rules version: {configuration.RulesVersion}");
        output.WriteLine($"Cache root: {paths.CacheRoot}");

        var rulesCachePath = ResolveRulesCachePath(configuration, paths);
        if (rulesCachePath is null || !File.Exists(rulesCachePath))
        {
            error.WriteLine("Resolved rule cache was not found.");
            if (!string.IsNullOrWhiteSpace(rulesCachePath))
            {
                error.WriteLine($"Cache: {rulesCachePath}");
            }

            return 9;
        }

        var ruleSet = new RuleSetCacheLoader().Load(File.ReadAllText(rulesCachePath));
        var validation = new RuleSetValidator().Validate(ruleSet);
        if (!validation.IsValid)
        {
            WriteValidationErrors(validation, error);
            return 8;
        }

        output.WriteLine($"Resolved rules version: {ruleSet!.RulesVersion}");
        output.WriteLine($"Rules cache: {rulesCachePath}");
        output.WriteLine("Status: healthy");
        return 0;
    }

    private static string? ResolveRulesCachePath(
        ZerberuzProjectConfiguration configuration,
        SharedRuleCachePaths paths)
    {
        if (!string.Equals(configuration.RulesVersion, "latest-compatible", StringComparison.OrdinalIgnoreCase))
        {
            return paths.RulesCachePath;
        }

        if (!File.Exists(paths.LatestCompatiblePointerPath))
        {
            return paths.LatestCompatiblePointerPath;
        }

        using var pointer = JsonDocument.Parse(File.ReadAllText(paths.LatestCompatiblePointerPath));
        return pointer.RootElement.TryGetProperty("RulesCachePath", out var pascalPath)
            ? pascalPath.GetString()
            : pointer.RootElement.TryGetProperty("rulesCachePath", out var camelPath)
                ? camelPath.GetString()
                : null;
    }

    private static string ReadSource(string source)
    {
        if (Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            using var client = new HttpClient();
            return client.GetStringAsync(uri).GetAwaiter().GetResult();
        }

        return File.ReadAllText(source);
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
        if (args.Contains("--offline", StringComparer.Ordinal))
        {
            return ExplainOffline(diagnosticId, args, output, error);
        }

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

    private static int ExplainOffline(
        string diagnosticId,
        string[] args,
        TextWriter output,
        TextWriter error)
    {
        var configPath = ResolveOption(args, "--config-path") ?? "zerberuz.json";
        if (!File.Exists(configPath))
        {
            error.WriteLine($"Configuration was not found: {configPath}");
            return 7;
        }

        var configuration = ZerberuzProjectConfiguration.Load(File.ReadAllText(configPath));
        var paths = new SharedCachePathResolver().Resolve(
            configuration,
            ResolveOption(args, "--cache-root"));

        var rulesCachePath = ResolveRulesCachePath(configuration, paths);
        if (string.IsNullOrWhiteSpace(rulesCachePath))
        {
            error.WriteLine("Resolved rule cache was not found.");
            return 9;
        }

        var helpPath = Path.Combine(
            Path.GetDirectoryName(rulesCachePath)!,
            "help",
            diagnosticId + ".md");

        if (!File.Exists(helpPath))
        {
            error.WriteLine($"Offline help was not found for diagnostic '{diagnosticId}'.");
            error.WriteLine($"Help: {helpPath}");
            return 10;
        }

        output.Write(File.ReadAllText(helpPath));
        return 0;
    }

    private static void AtomicWriteValidatedRuleSet(string path, string content)
    {
        var parsed = new RuleSetCacheLoader().Load(content);
        var validation = new RuleSetValidator().Validate(parsed);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException("Rule cache content failed validation before atomic write.");
        }

        AtomicWriteText(path, content);
    }

    private static void AtomicWriteText(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(tempPath, content);

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        File.Move(tempPath, path);
    }

    private static void WriteValidationErrors(RuleSetValidationResult validation, TextWriter error)
    {
        error.WriteLine("Rule source is invalid.");
        foreach (var validationError in validation.Errors)
        {
            error.WriteLine($"{validationError.Code}: {validationError.Message}");
        }
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

    private static void ExportHelpMarkdown(
        string helpDirectory,
        IEnumerable<DiagnosticHelpDefinition> helpDefinitions)
    {
        Directory.CreateDirectory(helpDirectory);
        foreach (var help in helpDefinitions)
        {
            if (string.IsNullOrWhiteSpace(help.DiagnosticId))
            {
                continue;
            }

            AtomicWriteText(
                Path.Combine(helpDirectory, help.DiagnosticId + ".md"),
                RenderHelpMarkdown(help));
        }
    }

    private static string RenderHelpMarkdown(DiagnosticHelpDefinition help)
    {
        using var writer = new StringWriter();
        writer.WriteLine($"# {help.DiagnosticId}: {help.Title}");
        WriteMarkdownSection(writer, "Summary", help.Summary);
        WriteMarkdownSection(writer, "Why", help.Why);
        WriteMarkdownSection(writer, "Trigger", help.Trigger);
        WriteMarkdownSection(writer, "Bad Example", help.BadExample);
        WriteMarkdownSection(writer, "Good Example", help.GoodExample);

        if (help.FixSteps.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("## Fix");
            for (var index = 0; index < help.FixSteps.Count; index++)
            {
                writer.WriteLine($"{index + 1}. {help.FixSteps[index]}");
            }
        }

        WriteMarkdownSection(writer, "Suppression", help.SuppressionGuidance);

        if (help.RelatedDiagnostics.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("## Related");
            foreach (var related in help.RelatedDiagnostics)
            {
                writer.WriteLine($"- {related}");
            }
        }

        return writer.ToString();
    }

    private static void WriteMarkdownSection(TextWriter writer, string title, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine($"## {title}");
        writer.WriteLine(value);
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
        output.WriteLine("  zerberuz init [--team default] [--profile default] [--config-path zerberuz.json]");
        output.WriteLine("  zerberuz sync-rules --source <file-or-url> [--config-path zerberuz.json] [--cache-root <path>]");
        output.WriteLine("  zerberuz doctor [--config-path zerberuz.json] [--cache-root <path>]");
        output.WriteLine("  zerberuz explain ZBZ001 [--cache-path .zerberuz/rules-cache.json]");
        output.WriteLine("  zerberuz explain ZBZ001 --offline [--config-path zerberuz.json] [--cache-root <path>]");
    }

    private static int WriteUnknownCommand(string command, TextWriter error)
    {
        error.WriteLine($"Unknown command: {command}");
        return 1;
    }
}
