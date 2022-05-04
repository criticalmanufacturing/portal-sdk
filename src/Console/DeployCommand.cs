using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using Cmf.CustomerPortal.Sdk.Console.Extensions;
using Cmf.Foundation.Common.Licenses.Enums;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
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

            Add(new Option<bool>(new[] { "--terminateOtherVersions", "-tov" }, Resources.DEPLOYMENT_TERMINATE_OTHER_VERSIONS_HELP));

            Handler = CommandHandler.Create((DeployParameters x) => DeployHandler(x));
        }

        protected override IEnumerable<IOptionExtension> ExtendWithRange()
        {
            List<IOptionExtension> extensions = new List<IOptionExtension>
            {
                new ReplaceTokensExtension(),
                new CommonParametersExtension()
            };
            return extensions;
        }

        public async Task DeployHandler(DeployParameters parameters)
        {
            // get new environment handler and run it
            CreateSession(parameters.Verbose);
            NewEnvironmentHandler newEnvironmentHandler = ServiceLocator.Get<NewEnvironmentHandler>();
            await newEnvironmentHandler.Run(parameters.Name, parameters.Parameters, (EnvironmentType)Enum.Parse(typeof(EnvironmentType), parameters.Type), parameters.Site, parameters.License, 
                parameters.Package,
                (DeploymentTarget)Enum.Parse(typeof(DeploymentTarget), parameters.Target), parameters.Output,
                parameters.ReplaceTokens, parameters.Interactive, parameters.CustomerInfrastructureName, parameters.Description, parameters.TemplateName, parameters.TerminateOtherVersions, false);
        }
    }
}
