using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.OutputObjects;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessOrchestration.ChangeSetManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.OutputObjects;
using Cmf.LightBusinessObjects.Infrastructure;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class PublishCommand : BaseCommand
    {

        public PublishCommand() : this("publish", "Publishes one or more Deployment Package(s) into Customer Portal")
        {

        }

        public PublishCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option(new string[] { "--path", "-p" }, "Path to Deployment Package Manifest file, or folder to a folder containing multiple manifest files")
            {
                Argument = new Argument<FileSystemInfo>().ExistingOnly(),
                IsRequired = true
            });

            Handler = CommandHandler.Create(typeof(Publish).GetMethod(nameof(Publish.PublishHandler))!);
        }
    }

    class Publish : BaseHandler
    {
        async public Task<int> PublishHandler(FileSystemInfo path, bool verbose, string destination, string token, string[] replaceTokens)
        {
            Configure(destination, token, verbose, replaceTokens);

            List<string> manifestsToUpload = new List<string>();

            if ((path.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                LogVerbose(string.Format("Fetching manifests from directory: {0}", path.FullName));

                manifestsToUpload.AddRange(Directory.GetFiles(path.FullName, "*.xml", SearchOption.AllDirectories));
            }
            else
            {
                manifestsToUpload.Add(path.FullName);
            }

            if (manifestsToUpload.Count == 0)
            {
                Log("No manifest found to upload.");
                return 1;
            }

            ChangeSet changeset = new ChangeSet();
            changeset.Name = "Deployment-Packages-" + Guid.NewGuid();
            changeset.Type = "General";
            changeset.MakeEffectiveOnApproval = true;

            CreateObjectInput createObjectInput = new CreateObjectInput();
            createObjectInput.Object = changeset;
            CreateObjectOutput changeSetOutput = await createObjectInput.CreateObjectAsync();
            changeset = changeSetOutput.Object as ChangeSet;

            LogVerbose(string.Format("Changeset created: {0}", changeset.Name));

            foreach (string manifestfile in manifestsToUpload)
            {
                LogVerbose(string.Format("Creating Deployment Package from file: {0}", manifestfile));

                string content = File.ReadAllText(manifestfile);

                if (Tokens?.Count > 0)
                {
                    LogVerbose($"Replacing tokens in manifest");
                    content = ReplaceTokens(content);
                }

                CreateDeploymentPackageInput deploymentPackageInput = new CreateDeploymentPackageInput();
                deploymentPackageInput.ChangeSet = changeset;
                deploymentPackageInput.Manifest = content;
                deploymentPackageInput.IgnoreLastServiceId = true;

                CreateDeploymentPackageOutput output = await deploymentPackageInput.CreateDeploymentPackageAsync();

                Log(string.Format("Deployment Package created: {0}", output.DeploymentPackage.Name));
                LogVerbose();
            }
            LogVerbose(string.Format("Requesting Changeset approval: {0}", changeset.Name));

            RequestChangeSetApprovalInput requestApproval = new RequestChangeSetApprovalInput();
            requestApproval.ChangeSet = changeset;
            requestApproval.IgnoreLastServiceId = true;

            await requestApproval.RequestChangeSetApprovalAsync();

            LogVerbose(string.Format("Changeset approved: {0}", changeset.Name));

            Log("Publish finished with success");

            return 0;
        }
    }
}
