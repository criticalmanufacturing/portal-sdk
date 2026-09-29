using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Commands.Create;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Legacy
{
    /// <summary>
    /// Legacy <c>createinfrastructure</c> command. Superseded by <c>create infrastructure</c>.
    /// </summary>
    class CreateInfrastructureCommand : CreateInfrastructureCommandBase
    {
        private readonly Option<string> nameOption = new("--name", "-n") { Description = Resources.InfrastructureNameHelp };

        public CreateInfrastructureCommand() : base("createinfrastructure", "Creates a customer Infrastructure")
        {
            Options.Add(nameOption);
        }

        protected override string GetName(ParseResult parseResult) => parseResult.GetValue(nameOption);
    }
}
