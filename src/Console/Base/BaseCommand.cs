using Cmf.CustomerPortal.Sdk.Common;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Base
{
    class BaseCommand : Command
    {
        protected BaseCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option(new[] { "--verbose", "-v" }, Resources.VERBOSE_HELP));
        }

        protected ISession CreateSession(bool verbose)
        {
            return new Session(verbose);
        }
    }
}
