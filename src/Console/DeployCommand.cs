using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using Cmf.CustomerPortal.Sdk.Console.Extensions;
using Cmf.Foundation.Common.Licenses.Enums;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class DeployCommand : BaseCommand
    {
        public DeployCommand() : this("deploy", "Creates and deploys a new Customer Environment")
        {
        }

        public DeployCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<string>(new[] { "--site", "-s", }, Resources.DEPLOYMENT_SITE_HELP)
            {
                IsRequired = true
            });

            Add(new Option<string>(new[] { "--package", "-pck", }, Resources.DEPLOYMENT_PACKAGE_HELP)
            {
                IsRequired = true
            });

            var licensesOpt = new Option<string[]>(
                aliases: ["--license", "-lic",],
                description: Resources.DEPLOYMENT_LICENSES_HELP,
                parseArgument: arg =>
                {
                    return arg.Tokens.Single().Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                })
            {
                IsRequired = true
            };
            licensesOpt.AddSuggestions("License 1,License 2");
            licensesOpt.AddValidator(optionResult =>
            {
                var licenses = optionResult.GetValueOrDefault<string[]>();
                if (licenses == null || licenses.Length == 0)
                {
                    optionResult.ErrorMessage = "Missing Software Licenses.";
                }

                return optionResult.ErrorMessage;
            });
            Add(licensesOpt);

            Add(new Option<bool>(new[] { "--terminateOtherVersions", "-tov" }, Resources.DEPLOYMENT_TERMINATE_OTHER_VERSIONS_HELP));

            Add(new Option<double?>(new[] { "--deploymentTimeoutMinutes", "-to", }, Resources.DEPLOYMENT_TIMEOUT_MINUTES));

            Add(new Option<double?>(new[] { "--deploymentTimeoutMinutesToGetSomeMBMsg", "-tombm", }, Resources.DEPLOYMENT_TIMEOUT_MINUTES_TO_GET_SOME_MB_MESSAGE));

            Add(new Option<bool>(new[] { "--terminateOtherVersionsRemove", "-tovr" }, Resources.DEPLOYMENT_TERMINATE_OTHER_VERSIONS_REMOVE_HELP));

            Add(new Option<bool>(new[] { "--terminateOtherVersionsRemoveVolumes", "-tovrv" }, Resources.DEPLOYMENT_TERMINATE_OTHER_VERSIONS_REMOVE_VOLUMES_HELP));

            Handler = CommandHandler.Create((DeployParameters x) => DeployHandler(x));
        }

        protected override IEnumerable<IOptionExtension> ExtendWithRange()
        {
            return new List<IOptionExtension>
            {
                new ReplaceTokensExtension(),
                new CommonParametersExtension()
            };
        }

        public async Task DeployHandler(DeployParameters parameters)
        {
            // get new environment handler and run it
            CreateSession(parameters.Verbose);
            NewEnvironmentHandler newEnvironmentHandler = ServiceLocator.Get<NewEnvironmentHandler>();
            await newEnvironmentHandler.Run(parameters.Name, parameters.Parameters, (EnvironmentType)Enum.Parse(typeof(EnvironmentType), parameters.Type), parameters.Site,
                parameters.License,
                parameters.Package,
                (DeploymentTarget)Enum.Parse(typeof(DeploymentTarget), parameters.Target), parameters.Output,
                parameters.ReplaceTokens, parameters.Interactive, parameters.CustomerInfrastructureName, parameters.Description, parameters.TerminateOtherVersions, false,
                parameters.DeploymentTimeoutMinutes, parameters.DeploymentTimeoutMinutesToGetSomeMBMsg ,parameters.TerminateOtherVersionsRemove, parameters.TerminateOtherVersionsRemoveVolumes);
        }
    }
}
