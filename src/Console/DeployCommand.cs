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
            Add(new Option<string>(new[] { "--name", "-n", }, "Name of the environment to create."));

            Add(new Option<FileInfo>(new string[] { "--parameters", "-params" }, "Path to parameters file that describes the environment.")
            {
                Argument = new Argument<FileInfo>().ExistingOnly(),
                IsRequired = true
            });

            var typeargument = new Argument<string>().FromAmong("Development", "Production", "Staging", "Testing");
            typeargument.SetDefaultValue("Development");

            Add(new Option<string>(new[] { "--type", "-type", }, "Type of the environment to create.")
            {
                Argument = typeargument
            });
            Add(new Option<string>(new[] { "--site", "-s", }, "Name of the Site assotiated with the environment.")
            {
                IsRequired = true
            });
            Add(new Option<string>(new[] { "--license", "-lic", }, "Name of the license to use for the environment")
            {
                IsRequired = true
            });
            Add(new Option<string>(new[] { "--package", "-pck", }, "Name of the package to use to create the environment")
            {
                IsRequired = true
            });

            var targetArgument = new Argument<string>().FromAmong("portainer", "dockerswarm");
            targetArgument.SetDefaultValue("dockerswarm");
            Add(new Option<string>(new[] { "--target", "-trg", }, "Target for the environment.")
            {
                Argument = targetArgument,
                IsRequired = true
            });

            Add(new Option<DirectoryInfo>(new string[] { "--output", "-o" }, "Path to Deployment Package Manifest file, or folder to a folder containing multiple manifest files"));

            Handler = CommandHandler.Create(typeof(DeployCommand).GetMethod(nameof(DeployCommand.DeployHandler)), this);
        }

        public async Task DeployHandler(bool verbose, string name, FileInfo parameters, string type, string site, string license, string package, string target, DirectoryInfo output, string[] replaceTokens)
        {
            // get new environment handler and run it
            var session = new Session(verbose);
            NewEnvironmentHandler newEnvironmentHandler = new NewEnvironmentHandler(new CustomerPortalClient(session), session);
            await newEnvironmentHandler.Run(name, parameters, (EnvironmentType)Enum.Parse(typeof(EnvironmentType), type), site, license, package, target, output);
        }
    }
}
