using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using Cmf.CustomerPortal.Sdk.Console.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Logs;

/// <summary>
/// <c>logs env &lt;name&gt; [--version &lt;version&gt;] [--type All|Installation|Termination] [--format Table|Json]
/// [--output &lt;path&gt;]</c>: shows the logs of a Customer Environment version, on the standard output or in a file.
/// </summary>
class LogsEnvironmentCommand : BaseCommand
{
    // column headers of the table, named after the fields of a log entry
    internal const string LogLevelHeader = "LogLevel";
    internal const string StepIdHeader = "StepId";
    internal const string DataHeader = "Data";

    /// <summary>
    /// Log level shown for an entry without one: the portal leaves it empty for information messages.
    /// </summary>
    internal const string DefaultLogLevel = "INFO";

    private readonly Argument<string> nameArgument = new("name") { Description = Resources.ArgExistingEnvironmentNameHelp };

    private readonly Option<EnvironmentLogType> typeOption = new("--type")
    {
        Description = Resources.LogTypeHelp,
        DefaultValueFactory = _ => EnvironmentLogType.All
    };

    private readonly Option<string> outputOption = new("--output", "-o")
    {
        Description = Resources.LogsOutputHelp,
        HelpName = "path"
    };

    private readonly EnvironmentVersionExtension version;
    private readonly OutputFormatExtension format;

    public LogsEnvironmentCommand() : base("env", Resources.NounLogsEnvironmentHelp)
    {
        Aliases.Add("environment");
        Arguments.Add(nameArgument);
        version = Use(new EnvironmentVersionExtension());
        Options.Add(typeOption);
        format = Use(new OutputFormatExtension());
        Options.Add(outputOption);

        SetAction(GetLogs);
    }

    public override string PreviewMessage => Resources.LogsEnvironmentPreview;

    private async Task GetLogs(ParseResult parseResult)
    {
        string name = parseResult.GetValue(nameArgument);
        int? versionNumber = parseResult.GetValue(version.Version);
        OutputFormat outputFormat = parseResult.GetValue(format.Format);

        CreateSession(parseResult);
        GetEnvironmentLogsHandler handler = ServiceLocator.Get<GetEnvironmentLogsHandler>();
        JsonObject logs = await handler.Run(name, versionNumber, parseResult.GetValue(typeOption));

        string output = parseResult.GetValue(outputOption);
        if (string.IsNullOrWhiteSpace(output))
        {
            WriteLogs(parseResult.InvocationConfiguration.Output, logs, outputFormat);
        }
        else
        {
            using var contents = new StringWriter();
            WriteLogs(contents, logs, outputFormat);
            handler.WriteLogs(contents.ToString(), output, name, versionNumber, outputFormat == OutputFormat.Json ? ".json" : ".log");
        }
    }

    private static void WriteLogs(TextWriter output, JsonObject logs, OutputFormat outputFormat)
    {
        if (outputFormat == OutputFormat.Json)
        {
            output.WriteLine(logs.ToJsonString(JsonOutput.Options));
        }
        else
        {
            WriteTable(output, logs);
        }
    }

    /// <summary>
    /// Writes each type of logs as a titled section of <c>LogLevel | StepId | Data</c> rows, with the columns aligned
    /// across the sections. Logs that are missing or empty are shown as <see cref="TableOutput.EmptyValue"/>, and logs that
    /// aren't a list of entries as they are.
    /// </summary>
    internal static void WriteTable(TextWriter output, JsonObject logs)
    {
        var sections = logs
            .Select(log => (Title: SectionTitle(log.Key), Logs: log.Value, Rows: (log.Value as JsonArray)?.Select(ToRow).ToList()))
            .ToList();

        LogRow[] rows = [new(LogLevelHeader, StepIdHeader, DataHeader), .. sections.SelectMany(section => section.Rows ?? [])];
        int levelWidth = rows.Max(row => row.LogLevel.Length);
        int stepWidth = rows.Max(row => row.StepId.Length);

        for (int i = 0; i < sections.Count; i++)
        {
            (string title, JsonNode sectionLogs, List<LogRow> sectionRows) = sections[i];
            if (i > 0)
            {
                output.WriteLine();
            }

            if (sectionLogs is null || sectionRows is { Count: 0 })
            {
                output.WriteLine($"{title}: {TableOutput.EmptyValue}");
                continue;
            }

            output.WriteLine($"{title}:");
            if (sectionRows is null)
            {
                output.WriteLine(Text(sectionLogs));
                continue;
            }

            foreach (LogRow row in sectionRows.Prepend(rows[0]))
            {
                WriteRow(output, row, levelWidth, stepWidth);
            }
        }
    }

    private static void WriteRow(TextWriter output, LogRow row, int levelWidth, int stepWidth)
    {
        // the lines after the first one of a multi-line entry stay in the Data column
        string[] lines = row.Data.TrimEnd('\r', '\n').Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string level = i == 0 ? row.LogLevel : string.Empty;
            string step = i == 0 ? row.StepId : string.Empty;
            output.WriteLine($"{level.PadRight(levelWidth)} | {step.PadRight(stepWidth)} | {lines[i].TrimEnd('\r')}");
        }
    }

    private static string SectionTitle(string key) => key switch
    {
        GetEnvironmentLogsHandler.InstallationKey => "Installation logs",
        GetEnvironmentLogsHandler.TerminationKey => "Termination logs",
        _ => key
    };

    private static LogRow ToRow(JsonNode entry) => entry is JsonObject fields
        ? new LogRow(LogLevelName(fields[LogLevelHeader]), Text(fields[StepIdHeader]), Text(fields[DataHeader]))
        : new LogRow(DefaultLogLevel, TableOutput.EmptyValue, Text(entry));

    /// <summary>
    /// Name of a log level, which the portal sends as a <see cref="LogLevel"/> number.
    /// </summary>
    private static string LogLevelName(JsonNode logLevel)
    {
        if (logLevel is not JsonValue value || !value.TryGetValue(out int level))
        {
            string text = logLevel is JsonValue textValue && textValue.TryGetValue(out string s) ? s : logLevel?.ToJsonString();
            return string.IsNullOrWhiteSpace(text) ? DefaultLogLevel : text.ToUpperInvariant();
        }

        return (LogLevel)level switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => DefaultLogLevel,
            LogLevel.Warning => "WARNING",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRITICAL",
            _ => level.ToString()
        };
    }

    private static string Text(JsonNode node)
    {
        string text = node is JsonValue value && value.TryGetValue(out string s) ? s : node?.ToJsonString(JsonOutput.Options);
        return string.IsNullOrWhiteSpace(text) ? TableOutput.EmptyValue : text;
    }

    private record LogRow(string LogLevel, string StepId, string Data);
}
