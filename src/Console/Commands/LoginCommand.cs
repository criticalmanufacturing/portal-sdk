using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console.Commands
{
    /// <summary>
    /// <c>login</c>: logs in to the CMF Portal.
    /// </summary>
    class LoginCommand : BaseCommand
    {
        private readonly Option<string> tokenOption = new("--token", "--pat", "-t") { Description = Resources.LoginPatHelp };

        public LoginCommand() : base("login", "Log in to the CMF Portal")
        {
            Options.Add(tokenOption);

            SetAction(LoginHandler);
        }

        private async Task LoginHandler(ParseResult parseResult)
        {
            // use login handler to save login information
            CreateSession(parseResult);
            LoginHandler loginHandler = ServiceLocator.Get<LoginHandler>();
            await loginHandler.Run(parseResult.GetValue(tokenOption));
        }
    }
}
