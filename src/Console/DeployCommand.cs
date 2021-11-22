using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using Cmf.CustomerPortal.Sdk.Console.Extensions;
using Cmf.Foundation.Common.Licenses.Enums;
using System;
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

            AddParameters();
            Add(new Option<string>(new[] { "--site", "-s", }, Resources.DEPLOYMENT_SITE_HELP)
            {
                IsRequired = true
            });

            Add(new Option<string>(new[] { "--package", "-pck", }, Resources.DEPLOYMENT_PACKAGE_HELP)
            {
                IsRequired = true
            });

            Handler = CommandHandler.Create(typeof(DeployCommand).GetMethod(nameof(DeployCommand.DeployHandler)), this);
        }

        protected override IOptionExtension ExtendWith()
        {
            return new ReplaceTokensExtension();
        }
        public async Task DeployHandler(bool verbose, string customerInfrastructureName, string name, string description, FileInfo parameters, string type, string site, string license,
            string package, string target, string templateName, DirectoryInfo output, string[] replaceTokens, bool interactive)
        {
            // get new environment handler and run it
            CreateSession(verbose);
            NewEnvironmentHandler newEnvironmentHandler = ServiceLocator.Get<NewEnvironmentHandler>();
            await newEnvironmentHandler.Run(name, parameters, (EnvironmentType)Enum.Parse(typeof(EnvironmentType), type), site, license, package, target, output,
                replaceTokens, interactive, customerInfrastructureName, description, templateName, false);
        }
    }
}
