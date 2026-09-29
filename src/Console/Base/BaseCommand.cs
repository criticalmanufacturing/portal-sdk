using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Extensions;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Base
{
    /// <summary>
    /// Base class for every command that talks to the Customer Portal. Adds the <c>--verbose</c> option and
    /// creates the session used by the Common handlers.
    /// </summary>
    abstract class BaseCommand : Command
    {
        protected IServiceLocator ServiceLocator
        {
            get; private set;
        }

        /// <summary>
        /// The <c>--verbose</c> option, available on every command.
        /// </summary>
        public Option<bool> VerboseOption { get; } = new("--verbose", "-v") { Description = Resources.VerboseHelp };

        protected BaseCommand(string name, string description = null) : base(name, description)
        {
            Options.Add(VerboseOption);
        }

        /// <summary>
        /// Adds the options of <paramref name="extension"/> to this command.
        /// </summary>
        /// <returns>The same extension, so its options can be read in the command action.</returns>
        protected T Use<T>(T extension) where T : IOptionExtension
        {
            extension.Use(this);
            return extension;
        }

        /// <summary>
        /// Creates the session and service locator, honouring the <c>--verbose</c> option.
        /// </summary>
        /// <param name="parseResult">The parse result of the invocation.</param>
        protected void CreateSession(ParseResult parseResult)
        {
            Session session = new Session(parseResult.GetValue(VerboseOption));
            ServiceLocator = new ServiceLocator(session);
        }
    }
}
