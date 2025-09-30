using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class TokenCommand : BaseCommand
    {
        public TokenCommand() : this("token", "Print the authentication token of the CMF Portal")
        {
        }

        public TokenCommand(string name, string description = null) : base(name, description)
        {
            Handler = CommandHandler.Create(typeof(TokenCommand).GetMethod(nameof(TokenCommand.TokenHandler)), this);
        }

        public async Task TokenHandler(bool verbose)
        {
            // use token handler to print the token information
            CreateSession(verbose);
            TokenHandler tokenHandler = ServiceLocator.Get<TokenHandler>();
            await tokenHandler.Run();
        }
    }
}
