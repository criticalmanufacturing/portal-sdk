using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Extensions;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
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
        /// Message printed when the command runs, telling it is a preview whose options and output may change.
        /// <c>null</c> when the command is not a preview.
        /// </summary>
        public virtual string PreviewMessage => null;

        private readonly Dictionary<Option, Func<string, string>> deprecatedOptions = [];

        /// <summary>
        /// Marks <paramref name="option"/> of this command as deprecated: when it is given, the message built by
        /// <paramref name="message"/> is printed.
        /// </summary>
        /// <param name="option">The deprecated option.</param>
        /// <param name="message">Builds the message naming the option's replacement, from the alias that was typed.</param>
        protected void DeprecateOption(Option option, Func<string, string> message)
        {
            deprecatedOptions[option] = message;
        }

        /// <summary>
        /// Warns that the command is deprecated or a preview, when it is, and about each deprecated option given, then
        /// creates the session and service locator, honouring the <c>--verbose</c> option.
        /// </summary>
        /// <param name="parseResult">The parse result of the invocation.</param>
        protected void CreateSession(ParseResult parseResult)
        {
            WarnIfDeprecated(parseResult.InvocationConfiguration.Error);
            WarnIfDeprecatedOptions(parseResult, parseResult.InvocationConfiguration.Error);
            WarnIfPreview(parseResult.InvocationConfiguration.Error);

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

        /// <summary>
        /// Writes the message of each option marked with <see cref="DeprecateOption"/> that <paramref name="parseResult"/>
        /// gives. Standard error keeps the command's output unchanged for scripts.
        /// </summary>
        /// <param name="parseResult">The parse result of the invocation.</param>
        /// <param name="error">The standard error of the invocation.</param>
        internal void WarnIfDeprecatedOptions(ParseResult parseResult, TextWriter error)
        {
            foreach ((Option option, Func<string, string> message) in deprecatedOptions)
            {
                if (parseResult.GetResult(option) is OptionResult { Implicit: false } optionResult)
                {
                    error.WriteLine(message(optionResult.IdentifierToken?.Value ?? option.Name));
                }
            }
        }

        /// <summary>
        /// Writes <see cref="PreviewMessage"/>, if any. Standard error keeps the command's output unchanged for scripts.
        /// </summary>
        /// <param name="error">The standard error of the invocation.</param>
        internal void WarnIfPreview(TextWriter error)
        {
            if (PreviewMessage != null)
            {
                error.WriteLine(PreviewMessage);
            }
        }
    }
}
