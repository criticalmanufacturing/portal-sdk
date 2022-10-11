using Cmf.CustomerPortal.Sdk.Common;
using Cmf.Foundation.Common.Licenses.Enums;
using System;
using System.CommandLine;
using System.IO;

namespace Cmf.CustomerPortal.Sdk.Console.Extensions
{
    class CommonParametersExtension : IOptionExtension
    {
        public void Use(Command command)
        {
            command.Add(new Option<string>(new[] { "--customer-infrastructure-name", "-ci", }, Resources.INFRASTRUCTURE_EXISTING_NAME_HELP));

            command.Add(new Option<string>(new[] {"--name", "-n", }, Resources.DEPLOYMENT_NAME_HELP));

            command.Add(new Option<string>(new[] { "--alias", "-a", }, Resources.DEPLOYMENT_ALIAS_HELP));

            command.Add(new Option<string>(new[] { "--description", "-d", }, Resources.DEPLOYMENT_DESCRIPTION_HELP));

            command.Add(new Option<FileInfo>(new string[] { "--parameters", "-params" }, Resources.DEPLOYMENT_PARAMETERSPATH_HELP)
            {
                Argument = new Argument<FileInfo>().ExistingOnly()
            });

            var typeargument = new Argument<string>().FromAmong(Enum.GetNames(typeof(EnvironmentType)));
            typeargument.SetDefaultValue(EnvironmentType.Development.ToString());
            command.Add(new Option<string>(new[] { "--type", "-type", }, Resources.DEPLOYMENT_ENVIRONMENTTYPE_HELP)
            {
                Argument = typeargument
            });

            var targetArgument = new Argument<string>().FromAmong(Enum.GetNames(typeof(DeploymentTarget)));
            targetArgument.SetDefaultValue(DeploymentTarget.dockerswarm.ToString());
            command.Add(new Option<string>(new[] { "--target", "-trg", }, Resources.DEPLOYMENT_TARGET_HELP)
            {
                Argument = targetArgument,
                IsRequired = true
            });

            command.Add(new Option<string>(new[] { "--template-name", "-template", }, Resources.INFRASTRUCTURE_EXISTING_ENVIRONMENT_TEMPLATE_NAME_HELP));

            command.Add(new Option<DirectoryInfo>(new string[] { "--output", "-o" }, Resources.DEPLOYMENT_OUTPUTDIR_HELP));

            command.Add(new Option<bool>(new[] { "--interactive", "-i" }, Resources.DEPLOYMENT_INTERACTIVE_HELP));

            command.Add(new Option<bool>(new[] { "--force", "-f" }, Resources.INFRASTRUCTURE_FORCE_HELP));

            command.Add(new Option<int>(new[] { "--timeout", "-to" }, Resources.INFRASTRUCTURE_CREATION_SECONDS_TIMEOUT_HELP));
        }
    }
}
