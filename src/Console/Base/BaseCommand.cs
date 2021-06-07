using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Base
{
    class BaseCommand : Command
    {
        public BaseCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option(new[] { "--verbose", "-v" }, "Show detailed logging"));
        }
    }
}
