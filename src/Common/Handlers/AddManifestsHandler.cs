using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.OutputObjects;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessOrchestration.ChangeSetManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class AddManifestsHandler(ISession session, IFileSystem fileSystem) : AbstractHandler(session, true)
    {
        public async Task Run(FileSystemInfo path, string datagroup, string[] replaceTokens)
        {
            await EnsureLogin();

            List<string> manifestsToUpload = new List<string>();

            if (path.Attributes.HasFlag(FileAttributes.Directory))
            {
                Session.LogDebug($"Fetching manifests from directory: {path.FullName}");

                manifestsToUpload.AddRange(fileSystem.Directory.GetFiles(path.FullName, "*.xml", SearchOption.AllDirectories));
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
            ChangeSet changeset = (await createObjectInput.CreateObjectAsync(true)).Object as ChangeSet;

            Session.LogDebug($"Changeset created: {changeset.Name}");

            foreach (string manifestfile in manifestsToUpload)
            {
                Session.LogDebug($"Creating Deployment Package from file: {manifestfile}");

                var content = await fileSystem.File.ReadAllTextAsync(manifestfile);
                content = await Utils.ReplaceTokens(Session, content, replaceTokens);

                CreateDeploymentPackageInput deploymentPackageInput = new CreateDeploymentPackageInput
                {
                    ChangeSet = changeset,
                    Manifest = content,
                    IgnoreLastServiceId = true,
                    DatagroupName = datagroup
                };

                CreateDeploymentPackageOutput output = await deploymentPackageInput.CreateDeploymentPackageAsync(true);

                Session.LogInformation($"Deployment Package created: {output.DeploymentPackage.Name}");
            }

            Session.LogDebug($"Requesting Changeset approval: {changeset.Name}");

            RequestChangeSetApprovalInput requestApproval = new RequestChangeSetApprovalInput
            {
                ChangeSet = changeset,
                IgnoreLastServiceId = true
            };

            await requestApproval.RequestChangeSetApprovalAsync(true);

            Session.LogDebug($"Changeset approved: {changeset.Name}");

            Session.LogInformation("Publish finished with success");
        }
    }
}
