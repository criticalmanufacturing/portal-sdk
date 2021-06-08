using Cmf.CustomerPortal.Sdk.Common;
using Microsoft.Extensions.Configuration;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Base
{
    class BaseCommand : Command
    {
        protected BaseCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option(new[] { "--verbose", "-v" }, "Show detailed logging"));
        }

        protected ISession CreateSession(bool verbose)
        {
            Session session = new Session(verbose);
            var serviceLocator = new ServiceLocator(session);
            session.Configuration = serviceLocator.Get<IConfiguration>();
            return session;
        }
    }
}
