using Cmf.CustomerPortal.Sdk.Common;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Base
{
    class BaseCommand : Command
    {
        protected IServiceLocator ServiceLocator
        {
            get; private set;
        }

        protected BaseCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<bool>(new[] { "--verbose", "-v" }, Resources.VERBOSE_HELP));
        }

        /// <summary>
        /// TODO: Rename this and make it void
        /// </summary>
        /// <param name="verbose"></param>
        /// <returns></returns>
        protected void CreateSession(bool verbose)
        {
            Session session = new Session(verbose);
            ServiceLocator = new ServiceLocator(session);
        }
    }
}
