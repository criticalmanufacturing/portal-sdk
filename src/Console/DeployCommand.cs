using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using Cmf.Foundation.Common.Licenses.Enums;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class DeployCommand : BaseCommand
    {
        public DeployCommand() : this("deploy", "Creates and deploys a new Customer Environment")
        {
        }

        public DeployCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<string>(new[] { "--name", "-n", }, Resources.DEPLOYMENT_NAME_HELP));

            Add(new Option<FileInfo>(new string[] { "--parameters", "-params" }, Resources.DEPLOYMENT_PARAMETERSPATH_HELP)
            {
                Argument = new Argument<FileInfo>().ExistingOnly(),
                IsRequired = true
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

            Add(new Option<string>(new[] { "--package", "-pck", }, Resources.DEPLOYMENT_PACKAGE_HELP)
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

            Add(new Option<DirectoryInfo>(new string[] { "--output", "-o" }, Resources.DEPLOYMENT_OUTPUTDIR_HELP));

            var replaceTokensOption = new Option<string[]>(new[] { "--replace-tokens" }, "Replace the tokens specified in the input files using the proper syntax (e.g. #{MyToken}#) with the specified values.")
            {
                AllowMultipleArgumentsPerToken = true
            };
            replaceTokensOption.AddSuggestions(new string[] { "MyToken=value MyToken2=value2" });
            Add(replaceTokensOption);

            Handler = CommandHandler.Create(typeof(DeployCommand).GetMethod(nameof(DeployCommand.DeployHandler)), this);
        }

        public async Task DeployHandler(bool verbose, string name, FileInfo parameters, string type, string site, string license, string package, string target, DirectoryInfo output, string[] replaceTokens)
        {
            // get new environment handler and run it
            var session = new Session(verbose);
            NewEnvironmentHandler newEnvironmentHandler = new NewEnvironmentHandler(new CustomerPortalClient(session), session);
            await newEnvironmentHandler.Run(name, parameters, (EnvironmentType)Enum.Parse(typeof(EnvironmentType), type), site, license, package, target, output, replaceTokens);
        }
    }
}
