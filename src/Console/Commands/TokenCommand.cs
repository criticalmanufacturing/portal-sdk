using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Commands;

/// <summary>
/// <c>token</c>: prints the configured authentication token for use in scripts.
/// </summary>
class TokenCommand : BaseCommand
{
    public TokenCommand() : base("token", Resources.TokenHelp)
    {
        SetAction(PrintToken);
    }

    private int PrintToken(ParseResult parseResult)
    {
        CreateSession(parseResult);
        try
        {
            string token = ServiceLocator.Get<ISession>().GetToken();
            parseResult.InvocationConfiguration.Output.WriteLine(token);
            return 0;
        }
        catch (InvalidOperationException exception)
        {
            parseResult.InvocationConfiguration.Error.WriteLine(exception.Message);
            return 1;
        }
    }
}
