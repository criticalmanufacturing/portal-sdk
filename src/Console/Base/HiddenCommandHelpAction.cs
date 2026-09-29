using System.CommandLine;
using System.CommandLine.Invocation;

namespace Cmf.CustomerPortal.Sdk.Console.Base
{
    /// <summary>
    /// Help action that also renders help for hidden commands. System.CommandLine prints nothing for a hidden
    /// command's <c>-h</c>, but the legacy commands are hidden only to keep them out of the root command list.
    /// </summary>
    /// <param name="helpAction">The default help action to wrap.</param>
    class HiddenCommandHelpAction(SynchronousCommandLineAction helpAction) : SynchronousCommandLineAction
    {
        /// <summary>
        /// Like the default help action, <c>-h</c> wins over parse errors such as missing required options.
        /// </summary>
        public override bool ClearsParseErrors => true;

        public override int Invoke(ParseResult parseResult)
        {
            Command command = parseResult.CommandResult.Command;
            bool hidden = command.Hidden;
            command.Hidden = false;
            try
            {
                return helpAction.Invoke(parseResult);
            }
            finally
            {
                command.Hidden = hidden;
            }
        }
    }
}
