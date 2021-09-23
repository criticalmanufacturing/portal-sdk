using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessOrchestration.EntityTypeManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.EntityTypeManagement.OutputObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.Foundation.Common.Licenses.Enums;
using Cmf.LightBusinessObjects.Infrastructure;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using Cmf.MessageBus.Messages;
using Cmf.Services.GenericServiceManagement;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class NewEnvironmentHandler : AbstractHandler
    {
        private readonly ICustomerPortalClient _customerPortalClient;
        private bool _isDeploymentFinished = false;
        private bool _hasDeploymentFailed = false;


        public NewEnvironmentHandler(ICustomerPortalClient customerPortalClient, ISession session) : base(session, true)
        {
            _customerPortalClient = customerPortalClient;
        }

        private string GetTargetValue(string targetInput)
        {
            switch (targetInput)
            {
                case "portainer":
                    return "PortainerV2Target";
                case "dockerswarm":
                    return "DockerSwarmOnPremisesTarget";
                default:
                    throw new Exception($"Target parameter '{targetInput}' not recognized");
            }
        }

        public async Task Run(
            string name,
            FileInfo parameters,
            EnvironmentType environmentType,
            string siteName,
            string licenseName,
            string deploymentPackageName,
            string target,
            DirectoryInfo outputDir,
            string[] replaceTokens,
            bool interactive
        )
        {
            await EnsureLogin();

            name = string.IsNullOrWhiteSpace(name) ? $"Deployment-{Guid.NewGuid()}" : name;
            string rawParameters = null;
            
            if (parameters != null) {
                rawParameters = File.ReadAllText(parameters.FullName);
                rawParameters = await Utils.ReplaceTokens(Session, rawParameters, replaceTokens, true);
            }

            Session.LogInformation($"Creating customer environment {name}...");

            // let's see if it exists
            CustomerEnvironment environment = null;
            try
            {
                environment = await _customerPortalClient.GetObjectByName<CustomerEnvironment>(name);
            }
            catch (CmfFaultException) { }

            // if it exists, maintain everything that is definition (name, type, site), change everything else and create new version
            if (environment != null)
            {
                environment.DeploymentPackage = await _customerPortalClient.GetObjectByName<DeploymentPackage>(deploymentPackageName);
                environment.CustomerLicense = await _customerPortalClient.GetObjectByName<CustomerLicense>(licenseName);
                environment.DeploymentTarget = GetTargetValue(target);
                environment.Parameters = rawParameters;
                environment.ChangeSet = null;
            }
            // if not, just build a new complete object and create it
            else
            {
                environment = new CustomerEnvironment
                {
                    EnvironmentType = environmentType.ToString(),
                    Site = await _customerPortalClient.GetObjectByName<ProductSite>(siteName),
                    Name = name,
                    DeploymentPackage = await _customerPortalClient.GetObjectByName<DeploymentPackage>(deploymentPackageName),
                    CustomerLicense = await _customerPortalClient.GetObjectByName<CustomerLicense>(licenseName),
                    DeploymentTarget = GetTargetValue(target),
                    Parameters = rawParameters
                };
            }
            environment = (await new CreateObjectVersionInput { Object = environment }.CreateObjectVersionAsync(true)).Object as CustomerEnvironment;

            var messageBus = await _customerPortalClient.GetMessageBusTransport();
            var subject = $"CE_DEPLOYMENT_{environment.Id}";

            Session.LogDebug($"Subscribing messagebus subject {subject}");
            messageBus.Subscribe(subject, ProcessDeploymentMessage);

            // start deployment
            var startDeploymentInput = new StartDeploymentInput
            {
                CustomerEnvironment = environment
            };

            if (interactive)
            {
                string infrastructureUrl = $"{(ClientConfigurationProvider.ClientConfiguration.UseSsl ? "https" : "http")}://{ClientConfigurationProvider.ClientConfiguration.HostAddress}/Entity/CustomerEnvironment/{environment.Id}/View/Installation";

                Session.LogInformation($"Environment {environment.Name} was created, please access the portal url to configure the environment and start the installation:");
                Session.LogInformation(infrastructureUrl);
                Session.LogInformation($"Waiting for user configuration...");
            }
            else
            {
                Session.LogInformation($"Starting deployment of environment {environment.Name}...");
                var result = await startDeploymentInput.StartDeploymentAsync(true);
            }

            // show progress from deployment
            TimeSpan timeout = TimeSpan.FromHours(1);
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeout))
            {
                while (!this._isDeploymentFinished)
                {
                    Session.LogPendingMessages();
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(0.1), cancellationTokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
            if (_hasDeploymentFailed)
            {
                throw new Exception("Deployment Failed! Check the logs for more information");
            }

            await ProcessEnvironmentDeployment(environment, target, outputDir);
        }

        private void ProcessDeploymentMessage(string subject, MbMessage message)
        {
            if (message != null && !string.IsNullOrWhiteSpace(message.Data))
            {
                var messageContentFormat = new { Data = string.Empty, DeploymentStatus = (DeploymentStatus?)DeploymentStatus.NotDeployed, StepId = string.Empty };
                var content = JsonConvert.DeserializeAnonymousType(message.Data, messageContentFormat);

                Session.LogInformation(content.Data);

                if (content.DeploymentStatus == DeploymentStatus.DeploymentFailed || content.DeploymentStatus == DeploymentStatus.DeploymentPartiallySucceeded || content.DeploymentStatus == DeploymentStatus.DeploymentSucceeded)
                {
                    if (content.DeploymentStatus == DeploymentStatus.DeploymentFailed)
                    {
                        _hasDeploymentFailed = true;
                    }
                    _isDeploymentFinished = true;
                }
            }
            else
            {
                Session.LogInformation("Unknown message received");
            }
        }

        private async Task ProcessEnvironmentDeployment(CustomerEnvironment environment, string target, DirectoryInfo outputPath)
        {
            switch (target)
            {
                case "dockerswarm":

                    // get the attachments of the current customer environment
                    GetAttachmentsForEntityInput input = new GetAttachmentsForEntityInput()
                    {
                        Entity = environment
                    };

                    await Task.Delay(TimeSpan.FromSeconds(1));
                    GetAttachmentsForEntityOutput output = await input.GetAttachmentsForEntityAsync(true);
                    EntityDocumentation attachmentToDownload = null;
                    if (output?.Attachments.Count > 0)
                    {
                        output.Attachments.Sort((a, b) => DateTime.Compare(b.CreatedOn, a.CreatedOn));
                        attachmentToDownload = output.Attachments.Where(x => x.Filename.Contains(environment.Name)).FirstOrDefault();
                    }

                    if (attachmentToDownload == null)
                    {
                        Session.LogError("No attachment was found to download.");
                    }
                    else
                    {
                        // Download the attachment
                        Session.LogDebug($"Downloading attachment {attachmentToDownload.Filename}");

                        string outputFile = "";
                        using (DownloadAttachmentStreamingOutput downloadAttachmentOutput = await new DownloadAttachmentStreamingInput() { attachmentId = attachmentToDownload.Id }.DownloadAttachmentAsync(true))
                        {
                            int bytesToRead = 10000;
                            byte[] buffer = new byte[bytesToRead];

                            outputFile = Path.Combine(Path.GetTempPath(), downloadAttachmentOutput.FileName);
                            Session.LogDebug($"Downloading to {outputFile}");

                            using (BinaryWriter streamWriter = new BinaryWriter(File.Open(outputFile, FileMode.Create, FileAccess.Write)))
                            {
                                int length;
                                do
                                {
                                    length = downloadAttachmentOutput.Stream.Read(buffer, 0, bytesToRead);
                                    streamWriter.Write(buffer, 0, length);
                                    buffer = new byte[bytesToRead];

                                } while (length > 0);
                            }
                        }

                        // create the dir to extract to
                        string extractionTarget = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                        Directory.CreateDirectory(extractionTarget);

                        Session.LogDebug($"Extracting environment contents to {extractionTarget}");

                        // extract the zip to the previously created dir
                        ZipFile.ExtractToDirectory(outputFile, extractionTarget);

                        // get target full dir
                        string outputPathFullName = outputPath?.FullName;
                        if (string.IsNullOrEmpty(outputPathFullName))
                        {
                            outputPathFullName = Path.Combine(Directory.GetCurrentDirectory(), "out", environment.Name);
                        }
                        else
                        {
                            outputPathFullName = Path.GetFullPath(outputPathFullName);
                        }

                        // ensure the output path exists
                        Directory.CreateDirectory(outputPathFullName);

                        Session.LogDebug($"Moving environment contents from {extractionTarget} to {outputPathFullName}");

                        // create all of the directories
                        foreach (string dirPath in Directory.GetDirectories(extractionTarget, "*", SearchOption.AllDirectories))
                        {
                            Directory.CreateDirectory(dirPath.Replace(extractionTarget, outputPathFullName));
                        }

                        // copy all the files & Replaces any files with the same name
                        foreach (string newPath in Directory.GetFiles(extractionTarget, "*.*", SearchOption.AllDirectories))
                        {
                            File.Copy(newPath, newPath.Replace(extractionTarget, outputPathFullName), true);
                        }

                        Session.LogInformation($"Customer environment created at {outputPathFullName}");
                    }

                    break;
                default:
                    break;
            }
        }
    }

}