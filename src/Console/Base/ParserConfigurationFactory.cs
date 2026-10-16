using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO.Abstractions;

namespace Cmf.CustomerPortal.Sdk.Console.Base;

/// <summary>
/// Creates the configuration used to parse the <c>cmf-portal</c> command line.
/// </summary>
static class ParserConfigurationFactory
{
    /// <summary>
    /// Creates a configuration that reads an <c>@</c> argument as a response file only when that file exists, so values
    /// that start with <c>@</c>, like the npm scoped name <c>@criticalmanufacturing\mes:12.0.0</c>, can be passed as
    /// positional arguments.
    /// </summary>
    /// <param name="fileSystem">File system where the response files are looked up.</param>
    /// <returns>The parser configuration.</returns>
    internal static ParserConfiguration Create(IFileSystem fileSystem)
    {
        var configuration = new ParserConfiguration();
        TryReplaceToken readResponseFile = configuration.ResponseFileTokenReplacer;

        configuration.ResponseFileTokenReplacer = (string tokenToReplace, out IReadOnlyList<string> replacementTokens, out string errorMessage) =>
        {
            if (readResponseFile != null && fileSystem.File.Exists(tokenToReplace))
            {
                return readResponseFile(tokenToReplace, out replacementTokens, out errorMessage);
            }

            // not a response file: keep the argument as it is
            replacementTokens = null;
            errorMessage = null;
            return false;
        };

        return configuration;
    }
}
