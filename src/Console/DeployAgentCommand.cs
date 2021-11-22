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
    class DeployAgent : BaseCommand
    {
        public DeployAgent() : this("deployagent", "Creates and deploys a new Infrastructure Agent")
        {
        }

        public DeployAgent(string name, string description = null) : base(name, description)
        {
            Add(new Option<string>(new[] { "--customer-infrastructure-name", "-ci", }, Resources.INFRASTRUCTURE_EXISTING_NAME_HELP));

            Add(new Option<string>(new[] { "--name", "-n", }, Resources.DEPLOYMENT_NAME_HELP));

            Add(new Option<string>(new[] { "--description", "-d", }, Resources.DEPLOYMENT_NAME_HELP));

            Add(new Option<FileInfo>(new string[] { "--parameters", "-params" }, Resources.DEPLOYMENT_PARAMETERSPATH_HELP)
            {
                Argument = new Argument<FileInfo>().ExistingOnly()
            });

            var typeargument = new Argument<string>().FromAmong(Enum.GetNames(typeof(EnvironmentType)));
            typeargument.SetDefaultValue(EnvironmentType.Development.ToString());
            Add(new Option<string>(new[] { "--type", "-type", }, Resources.DEPLOYMENT_ENVIRONMENTTYPE_HELP)
            {
                Argument = typeargument
            });

            Add(new Option<string>(new[] { "--site", "-s", }, Resources.DEPLOYMENT_SITE_HELP)
            {
                IsRequired = true
            });

            Add(new Option<string>(new[] { "--license", "-lic", }, Resources.DEPLOYMENT_LICENSE_HELP)
            {
                IsRequired = true
            });

            var targetArgument = new Argument<string>().FromAmong("portainer", "dockerswarm");
            targetArgument.SetDefaultValue("dockerswarm");
            Add(new Option<string>(new[] { "--target", "-trg", }, Resources.DEPLOYMENT_TARGET_HELP)
            {
                Argument = targetArgument,
                IsRequired = true
            });

            Add(new Option<string>(new[] { "--template-name", "-template", }, Resources.INFRASTRUCTURE_EXISTING_ENVIRONMENT_TEMPLATE_NAME_HELP));

            Add(new Option<DirectoryInfo>(new string[] { "--output", "-o" }, Resources.DEPLOYMENT_OUTPUTDIR_HELP));

            Add(new Option<bool>(new[] { "--interactive", "-i" }, Resources.DEPLOYMENT_INTERACTIVE_HELP));

            Handler = CommandHandler.Create(typeof(DeployAgent).GetMethod(nameof(DeployAgent.DeployHandler)), this);
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
            await newEnvironmentHandler.Run(name, parameters, (EnvironmentType)Enum.Parse(typeof(EnvironmentType), type), site, license, null, target, output,
                replaceTokens, interactive, customerInfrastructureName, description, templateName, true);
        }
    }
}
