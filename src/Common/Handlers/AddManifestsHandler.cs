using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.OutputObjects;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessOrchestration.ChangeSetManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class AddManifestsHandler : AbstractHandler
    {
        private readonly ICustomerPortalClient _customerPortalClient;

        public AddManifestsHandler(ICustomerPortalClient customerPortalClient, ISession session) : base(session, true)
        {
            _customerPortalClient = customerPortalClient;
        }

        public async Task Run(FileSystemInfo path, string[] replaceTokens)
        {
            await LoginIfRequired();

            List<string> manifestsToUpload = new List<string>();

            if ((path.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                Session.LogDebug($"Fetching manifests from directory: {path.FullName}");

                manifestsToUpload.AddRange(Directory.GetFiles(path.FullName, "*.xml", SearchOption.AllDirectories));
            }
            else
            {
                manifestsToUpload.Add(path.FullName);
            }

            if (manifestsToUpload.Count == 0)
            {
                Session.LogInformation("No manifest found to upload.");
                return;
            }

            CreateObjectInput createObjectInput = new CreateObjectInput
            {
                Object = new ChangeSet
                {
                    Name = "Deployment-Packages-" + Guid.NewGuid(),
                    Type = "General",
                    MakeEffectiveOnApproval = true
                }
            };
            ChangeSet changeset = (await createObjectInput.CreateObjectAsync()).Object as ChangeSet;

            Session.LogDebug($"Changeset created: {changeset.Name}");

            foreach (string manifestfile in manifestsToUpload)
            {
                Session.LogDebug($"Creating Deployment Package from file: {manifestfile}");

                string content = File.ReadAllText(manifestfile);

                //if (Tokens?.Count > 0)
                //{
                //    LogVerbose($"Replacing tokens in manifest");
                //    content = ReplaceTokens(content);
                //}

                CreateDeploymentPackageInput deploymentPackageInput = new CreateDeploymentPackageInput
                {
                    ChangeSet = changeset,
                    Manifest = content,
                    IgnoreLastServiceId = true
                };

                CreateDeploymentPackageOutput output = await deploymentPackageInput.CreateDeploymentPackageAsync();

                Session.LogInformation($"Deployment Package created: {output.DeploymentPackage.Name}");
            }
            Session.LogDebug($"Requesting Changeset approval: {changeset.Name}");

            RequestChangeSetApprovalInput requestApproval = new RequestChangeSetApprovalInput
            {
                ChangeSet = changeset,
                IgnoreLastServiceId = true
            };

            await requestApproval.RequestChangeSetApprovalAsync();

            Session.LogDebug($"Changeset approved: {changeset.Name}");

            Session.LogInformation("Publish finished with success");
        }
    }
}
