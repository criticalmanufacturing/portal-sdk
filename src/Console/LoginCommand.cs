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
            Add(new Option<string>(new[] { "--token", "--pat", "-t", }, "Use the provided personal access token to publish in customer portal"));

            Handler = CommandHandler.Create(typeof(LoginCommand).GetMethod(nameof(LoginCommand.LoginHandler)), this);
        }

        public async Task LoginHandler(bool verbose, string token)
        {
            // use login handler to save login information
            LoginHandler loginHandler = new LoginHandler(new Session(verbose));
            await loginHandler.Run(token);
        }
    }
}
