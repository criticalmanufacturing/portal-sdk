using Cmf.CustomerPortal.Sdk.Common;
using Cmf.Foundation.Common.Licenses.Enums;
using System;
using System.CommandLine;
using System.IO;

namespace Cmf.CustomerPortal.Sdk.Console.Extensions
{
    /// <summary>
    /// Options shared by the commands that deploy a Customer Environment or an Infrastructure Agent.
    /// </summary>
    /// <param name="includeNameOption">
    /// Whether to add <c>--name</c>. Commands that take the name as a positional argument pass <c>false</c>.
    /// </param>
    class CommonParametersExtension(bool includeNameOption = true) : IOptionExtension
    {
        public Option<string> CustomerInfrastructureName { get; } = new("--customer-infrastructure-name", "-ci") { Description = Resources.InfrastructureExistingNameHelp };

        public Option<string> Name { get; } = new("--name", "-n") { Description = Resources.DeploymentNameHelp };

        public Option<string> Alias { get; } = new("--alias", "-a") { Description = Resources.DeploymentAliasHelp };

        public Option<string> Description { get; } = new("--description", "-d") { Description = Resources.DeploymentDescriptionHelp };

        public Option<FileInfo> Parameters { get; } = new Option<FileInfo>("--parameters", "-params") { Description = Resources.DeploymentParametersPathHelp }.AcceptExistingOnly();

        public Option<string> Type { get; } = CreateTypeOption();

        public Option<string> Target { get; } = CreateTargetOption();

        public Option<DirectoryInfo> Output { get; } = new("--output", "-o") { Description = Resources.DeploymentOutputDirHelp };

        public Option<bool> Interactive { get; } = new("--interactive", "-i") { Description = Resources.DeploymentInteractiveHelp };

        public void Use(Command command)
        {
            command.Options.Add(CustomerInfrastructureName);

            if (includeNameOption)
            {
                command.Options.Add(Name);
            }

            command.Options.Add(Alias);
            command.Options.Add(Description);
            command.Options.Add(Parameters);
            command.Options.Add(Type);
            command.Options.Add(Target);
            command.Options.Add(Output);
            command.Options.Add(Interactive);
        }

        /// <summary>
        /// Reads <c>--type</c> as an <see cref="EnvironmentType"/>.
        /// </summary>
        public EnvironmentType GetEnvironmentType(ParseResult parseResult)
        {
            return Enum.Parse<EnvironmentType>(parseResult.GetValue(Type));
        }

        /// <summary>
        /// Reads <c>--target</c> as a <see cref="DeploymentTarget"/>, or <c>null</c> when it was not provided.
        /// </summary>
        public DeploymentTarget? GetDeploymentTarget(ParseResult parseResult)
        {
            string target = parseResult.GetValue(Target);
            return string.IsNullOrWhiteSpace(target) ? null : Enum.Parse<DeploymentTarget>(target);
        }

        private static Option<string> CreateTypeOption()
        {
            var option = new Option<string>("--type", "-type")
            {
                Description = Resources.DeploymentEnvironmentTypeHelp,
                DefaultValueFactory = _ => EnvironmentType.Development.ToString()
            };
            option.AcceptOnlyFromAmong(Enum.GetNames<EnvironmentType>());
            return option;
        }

        private static Option<string> CreateTargetOption()
        {
            var option = new Option<string>("--target", "-trg")
            {
                Description = Resources.DeploymentTargetHelp,
                Required = false
            };
            option.AcceptOnlyFromAmong(Enum.GetNames<DeploymentTarget>());
            return option;
        }
    }
}
