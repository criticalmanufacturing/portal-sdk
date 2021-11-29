using Cmf.CustomerPortal.Sdk.Common;
using Cmf.Foundation.Common.Licenses.Enums;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Text;

namespace Cmf.CustomerPortal.Sdk.Console.Extensions
{
    class CommonParametersExtension : IOptionExtension
    {
        public void Use(Command command)
        {
            command.Add(new Option<string>(new[] { "--customer-infrastructure-name", "-ci", }, Resources.INFRASTRUCTURE_EXISTING_NAME_HELP));

            command.Add(new Option<string>(new[] { "--name", "-n", }, Resources.DEPLOYMENT_NAME_HELP));

            command.Add(new Option<string>(new[] { "--description", "-d", }, Resources.DEPLOYMENT_NAME_HELP));

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

            command.Add(new Option<string>(new[] { "--license", "-lic", }, Resources.DEPLOYMENT_LICENSE_HELP)
            {
                IsRequired = true
            });
            var targetArgument = new Argument<string>().FromAmong("portainer", "dockerswarm");
            targetArgument.SetDefaultValue("dockerswarm");
            command.Add(new Option<string>(new[] { "--target", "-trg", }, Resources.DEPLOYMENT_TARGET_HELP)
            {
                Argument = targetArgument,
                IsRequired = true
            });

            command.Add(new Option<string>(new[] { "--template-name", "-template", }, Resources.INFRASTRUCTURE_EXISTING_ENVIRONMENT_TEMPLATE_NAME_HELP));

            command.Add(new Option<DirectoryInfo>(new string[] { "--output", "-o" }, Resources.DEPLOYMENT_OUTPUTDIR_HELP));

            command.Add(new Option<bool>(new[] { "--interactive", "-i" }, Resources.DEPLOYMENT_INTERACTIVE_HELP));
        }
    }
}
