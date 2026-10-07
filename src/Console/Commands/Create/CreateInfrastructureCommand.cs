using Cmf.CustomerPortal.Sdk.Common;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Create
{
    /// <summary>
    /// <c>create infrastructure [name]</c>: creates a Customer Infrastructure.
    /// </summary>
    class CreateInfrastructureCommand : CreateInfrastructureCommandBase
    {
        private readonly Argument<string> nameArgument = new("name")
        {
            Description = Resources.InfrastructureNameHelp,
            Arity = ArgumentArity.ZeroOrOne
        };

        public CreateInfrastructureCommand() : base("infrastructure", Resources.NounCreateInfrastructureHelp)
        {
            Aliases.Add("infra");
            Arguments.Add(nameArgument);
        }

        protected override string GetName(ParseResult parseResult) => parseResult.GetValue(nameArgument);
    }
}
