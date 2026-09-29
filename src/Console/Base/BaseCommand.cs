using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Extensions;
using System.CommandLine;
using System.IO;

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
        /// Message printed when the command runs, telling it is deprecated and naming its replacement.
        /// <c>null</c> when the command is not deprecated.
        /// </summary>
        public virtual string DeprecationMessage => null;

        /// <summary>
        /// Warns that the command is deprecated, when it is, then creates the session and service locator,
        /// honouring the <c>--verbose</c> option.
        /// </summary>
        /// <param name="parseResult">The parse result of the invocation.</param>
        protected void CreateSession(ParseResult parseResult)
        {
            WarnIfDeprecated(parseResult.InvocationConfiguration.Error);

            Session session = new Session(parseResult.GetValue(VerboseOption));
            ServiceLocator = new ServiceLocator(session);
        }

        /// <summary>
        /// Writes <see cref="DeprecationMessage"/>, if any. Standard error keeps the command's output unchanged for scripts.
        /// </summary>
        /// <param name="error">The standard error of the invocation.</param>
        internal void WarnIfDeprecated(TextWriter error)
        {
            if (DeprecationMessage != null)
            {
                error.WriteLine(DeprecationMessage);
            }
        }
    }
}
