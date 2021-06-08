using Cmf.CustomerPortal.Sdk.Common;
using Microsoft.Extensions.Configuration;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Base
{
    class EnvironmentCommand : BaseCommand
    {
        public EnvironmentCommand(string name, string description) : base(name, description)
        {
            Add(new Option<string>(new string[] { "--destination", "--dest" }, Resources.DESTINATIONENVIRONMENT_HELP));
        }

        protected ISession CreateSession(bool verbose, string environment)
        {
            Session session = new Session(verbose);
            var serviceLocator = new ServiceLocator(session, environment);
            session.Configuration = serviceLocator.Get<IConfiguration>();
            return session;
        }
    }
}
