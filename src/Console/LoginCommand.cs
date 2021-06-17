using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class LoginCommand : BaseCommand
    {
        public LoginCommand() : this("login", "Log in to the CMF Portal")
        {
        }

        public LoginCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<string>(new[] { "--token", "--pat", "-t", }, Resources.LOGIN_PAT_HELP));

            Handler = CommandHandler.Create(typeof(LoginCommand).GetMethod(nameof(LoginCommand.LoginHandler)), this);
        }

        public async Task LoginHandler(bool verbose, string token)
        {
            // use login handler to save login information
            var session = CreateSession(verbose);
            LoginHandler loginHandler = new LoginHandler(session);
            await loginHandler.Run(token);
        }
    }
}
