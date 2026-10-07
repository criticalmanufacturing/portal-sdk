using Cmf.CustomerPortal.Sdk.Console;
using System.CommandLine;

namespace Console.UnitTests;

/// <summary>
/// Parses command lines against the real <c>cmf-portal</c> command tree. Provides an existing file and folder,
/// substituted for the <c>{file}</c> and <c>{dir}</c> placeholders, for options that only accept existing paths.
/// </summary>
public sealed class CommandLineFixture : IDisposable
{
    public string ExistingDirectory { get; }

    public string ExistingFile { get; }

    public CommandLineFixture()
    {
        ExistingDirectory = Directory.CreateTempSubdirectory("cmf-portal-tests").FullName;
        ExistingFile = Path.Combine(ExistingDirectory, "parameters.json");
        File.WriteAllText(ExistingFile, "{}");
    }

    internal ParseResult Parse(string commandLine)
    {
        return RootCommandFactory.Create().Parse(commandLine.Replace("{file}", ExistingFile).Replace("{dir}", ExistingDirectory));
    }

    internal static string Help(string commandLine)
    {
        using var output = new StringWriter();
        RootCommandFactory.Create().Parse(commandLine).Invoke(new InvocationConfiguration { Output = output, Error = output });
        return output.ToString();
    }

    public void Dispose()
    {
        Directory.Delete(ExistingDirectory, recursive: true);
    }
}
