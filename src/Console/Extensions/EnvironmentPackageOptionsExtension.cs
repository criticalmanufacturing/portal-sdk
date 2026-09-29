using Cmf.CustomerPortal.Sdk.Common;
using System;
using System.CommandLine;
using System.Linq;

namespace Cmf.CustomerPortal.Sdk.Console.Extensions
{
    /// <summary>
    /// Site, Deployment Package and License options used when deploying a Customer Environment.
    /// </summary>
    class EnvironmentPackageOptionsExtension : IOptionExtension
    {
        public Option<string> Site { get; } = new("--site", "-s") { Description = Resources.DeploymentSiteHelp, Required = false };

        public Option<string> Package { get; } = new("--package", "-pck") { Description = Resources.DeploymentPackageHelp, Required = false };

        public Option<string[]> License { get; } = CreateLicenseOption();

        public void Use(Command command)
        {
            command.Options.Add(Site);
            command.Options.Add(Package);
            command.Options.Add(License);
        }

        private static Option<string[]> CreateLicenseOption()
        {
            var option = new Option<string[]>("--license", "-lic")
            {
                Description = Resources.DeploymentLicensesHelp,
                HelpName = "License 1,License 2",
                Required = false,
                // a single comma-separated token, so a following positional argument is not consumed as a license
                Arity = ArgumentArity.ExactlyOne,
                CustomParser = result => result.Tokens.Single().Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            };
            option.Validators.Add(optionResult =>
            {
                var licenses = optionResult.GetValueOrDefault<string[]>();
                if (licenses == null || licenses.Length == 0)
                {
                    optionResult.AddError("Missing Software Licenses.");
                }
            });
            return option;
        }
    }
}
