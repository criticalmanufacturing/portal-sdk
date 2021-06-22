using Cmf.CustomerPortal.Sdk.Common;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Base
{
    class BaseCommand : Command
    {
        protected BaseCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<bool>(new[] { "--verbose", "-v" }, Resources.VERBOSE_HELP));
        }

        protected ISession CreateSession(bool verbose)
        {
            Session session = new Session(verbose);
            var serviceLocator = new ServiceLocator(session);
            return session;
        }
    }
}
