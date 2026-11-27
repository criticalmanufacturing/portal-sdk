using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;

namespace Cmf.CustomerPortal.Sdk.Console.Commands;

/// <summary>
/// <c>login</c>: logs in to the CMF Portal.
/// </summary>
class LoginCommand : BaseCommand
{
    private readonly Option<string> tokenOption = new("--token", "--pat", "-t")
    {
        Description = Resources.LoginPatHelp
    };

    private readonly Option<Uri> redirectUriOption = new("--redirect-uri")
    {
        Description = Resources.LoginRedirectUriHelp
    };

    public LoginCommand() : base("login", "Log in to the CMF Portal")
    {
        Options.Add(tokenOption);
        Options.Add(redirectUriOption);
        redirectUriOption.Validators.Add(ValidateRedirectUri);
        Validators.Add(RejectRedirectUriWithToken);

        SetAction(LoginHandler);
    }

    private async Task LoginHandler(ParseResult parseResult)
    {
        CreateSession(parseResult);
        LoginHandler loginHandler = ServiceLocator.Get<LoginHandler>();

        Uri redirectUri = parseResult.GetValue(redirectUriOption);
        if (redirectUri is null)
        {
            await loginHandler.Run(parseResult.GetValue(tokenOption));
            return;
        }

        TextWriter output = parseResult.InvocationConfiguration.Output;
        string refreshToken = await loginHandler.RunWithRedirect(
            redirectUri,
            authorizationUrl => WaitForCallback(output, System.Console.In, authorizationUrl)
        );
        WriteRefreshToken(output, refreshToken);
    }

    /// <summary>
    /// Writes the sign-in URL as JSON and reads the callback URL, one line, from <paramref name="input"/>.
    /// </summary>
    /// <returns>The callback URL, or <c>null</c> when <paramref name="input"/> was closed.</returns>
    internal static async Task<string> WaitForCallback(TextWriter output, TextReader input, Uri authorizationUrl)
    {
        var signIn = new { AuthorizationUrl = authorizationUrl.AbsoluteUri };
        output.WriteLine(JsonSerializer.Serialize(signIn, JsonOutput.Options));
        return await input.ReadLineAsync();
    }

    /// <summary>
    /// Writes the refresh token as JSON.
    /// </summary>
    internal static void WriteRefreshToken(TextWriter output, string refreshToken)
    {
        output.WriteLine(JsonSerializer.Serialize(new { RefreshToken = refreshToken }, JsonOutput.Options));
    }

    private static void ValidateRedirectUri(OptionResult result)
    {
        if (result.Tokens.Count != 1)
        {
            return;
        }

        if (!Uri.TryCreate(result.Tokens[0].Value, UriKind.Absolute, out Uri uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            result.AddError(Resources.LoginRedirectUriInvalidMessage);
        }
    }

    private void RejectRedirectUriWithToken(CommandResult result)
    {
        if (result.GetResult(tokenOption) is not null && result.GetResult(redirectUriOption) is not null)
        {
            result.AddError(Resources.LoginRedirectUriWithTokenMessage);
        }
    }
}
