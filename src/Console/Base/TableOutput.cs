using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cmf.CustomerPortal.Sdk.Console.Base;

/// <summary>
/// Conventions of the <see cref="OutputFormat.Table"/> output.
/// </summary>
static class TableOutput
{
    /// <summary>
    /// Value shown for an empty field.
    /// </summary>
    internal const string EmptyValue = "-";

    /// <summary>
    /// Writes the rows as <c>Label: value</c> lines with aligned values, with <see cref="EmptyValue"/> for the
    /// <c>null</c> ones.
    /// </summary>
    /// <param name="output">Where to write the rows.</param>
    /// <param name="rows">The label and value of each row.</param>
    internal static void WriteRows(TextWriter output, IReadOnlyCollection<(string Label, string Value)> rows)
    {
        int width = rows.Max(row => row.Label.Length) + 1;
        foreach ((string label, string value) in rows)
        {
            output.WriteLine($"{(label + ":").PadRight(width)} {value ?? EmptyValue}");
        }
    }

    /// <summary>
    /// Formats a date for a row.
    /// </summary>
    /// <returns>The date as <c>yyyy-MM-dd HH:mm:ss</c>, or <c>null</c> when it is not set.</returns>
    internal static string FormatDate(DateTime date) => date == default ? null : date.ToString("yyyy-MM-dd HH:mm:ss");
}
