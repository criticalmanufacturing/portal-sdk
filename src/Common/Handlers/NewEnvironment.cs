using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessOrchestration.EntityTypeManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.MessageBus.Messages;
using Cmf.Services.GenericServiceManagement;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class NewEnvironment : AbstractHandler
    {
        private readonly ICustomerPortalClient customerPortalClient;
        private bool isDeploymentFinished = false;
        private bool hasDeploymentFailed = false;

        public NewEnvironment(ICustomerPortalClient customerPortalClient, ISession session) : base(session)
        {
            this.customerPortalClient = customerPortalClient;
        }

        public async Task Run(
            FileInfo parameters,
            string environemntType,
            string siteName,
            string licenseName,
            string deploymentPackageName,
            string target
        )
        {
            await base.Run();

            string name = $"Deployment-{Guid.NewGuid()}";
            string rawParameters = File.ReadAllText(parameters.FullName);

            this.Session.LogDebug($"Creating customer environment {name}...");

            CustomerEnvironment environment = new CustomerEnvironment
            {
                EnvironmentType = environemntType,
                Site = await customerPortalClient.GetObjectByName<ProductSite>(siteName),
                Name = name,
                DeploymentPackage = await customerPortalClient.GetObjectByName<DeploymentPackage>(deploymentPackageName),
                CustomerLicense = await customerPortalClient.GetObjectByName<CustomerLicense>(licenseName),
                DeploymentTarget = null, //GetTargetValue(target),
                Parameters = rawParameters
            };

            var createInput = new CreateObjectInput
            {
                Object = environment
            };

            environment = (await createInput.CreateObjectAsync()).Object as CustomerEnvironment;

            var startDeploymentInput = new StartDeploymentInput();
            startDeploymentInput.CustomerEnvironment = environment;

            var messageBus = await customerPortalClient.GetMessageBusTransport();
            var subject = $"CE_DEPLOYMENT_{environment.Id}";

            this.Session.LogDebug($"Subscribing messagebus subject {subject}");
            messageBus.Subscribe(subject, ProcessDeploymentMessage);

            this.Session.LogInformation($"Starting deployment of environment {environment.Name}...");
            var result = await startDeploymentInput.StartDeploymentAsync();

            long elapsed = 0;
            TimeSpan timeout = new TimeSpan(1, 0, 0);
            while (!this.isDeploymentFinished && (elapsed < timeout.TotalMilliseconds))
            {
                await Task.Delay(100);
                elapsed += 100;
            }

            if (hasDeploymentFailed)
            {
                throw new Exception("Deployment Failed! Check the logs for more information");
            }

            await ProcessEnvironmentDeployment(environment, target, null);
        }

        private void ProcessDeploymentMessage(string subject, MbMessage message)
        {
            if (message != null && !string.IsNullOrWhiteSpace(message.Data))
            {

                var messageContentFormat = new { Data = String.Empty, DeploymentStatus = (DeploymentStatus?)DeploymentStatus.NotDeployed, StepId = String.Empty };
                var content = JsonConvert.DeserializeAnonymousType(message.Data, messageContentFormat);

                this.Session.LogDebug(content.Data);

                if (content.DeploymentStatus == DeploymentStatus.DeploymentFailed || content.DeploymentStatus == DeploymentStatus.DeploymentPartiallySucceeded || content.DeploymentStatus == DeploymentStatus.DeploymentSucceeded)
                {
                    if (content.DeploymentStatus == DeploymentStatus.DeploymentFailed)
                    {
                        this.hasDeploymentFailed = true;
                    }
                    this.isDeploymentFinished = true;
                }
            }
            else
            {
                this.Session.LogDebug("Unkown message received");
            }

        }

        private async Task<int> ProcessEnvironmentDeployment(CustomerEnvironment environment, string target, DirectoryInfo outputPath)
        {

            switch (target)
            {
                case "dockerswarm":

                    // download the attachment
                    GetAttachmentsForEntityInput input = new GetAttachmentsForEntityInput()
                    {
                        Entity = environment
                    };

                    await Task.Delay(1000);
                    var output = await input.GetAttachmentsForEntityAsync();
                    EntityDocumentation attachmentToDownload = null;
                    if (output?.Attachments.Count > 0)
                    {
                        output.Attachments.Sort((a, b) => DateTime.Compare(b.CreatedOn, a.CreatedOn));
                        attachmentToDownload = output.Attachments.Where(x => x.Filename.Contains(environment.Name)).FirstOrDefault();
                    }

                    if (attachmentToDownload == null)
                    {
                        this.Session.LogDebug("No attachment was found to download.");
                        return 1;
                    }
                    else
                    {
                        //Download Attachment
                        this.Session.LogDebug($"Downloading attachment {attachmentToDownload.Filename}");

                        string outputFile = "";
                        using (DownloadAttachmentStreamingOutput downloadAttachmentOutput = new DownloadAttachmentStreamingInput() { attachmentId = attachmentToDownload.Id }.DownloadAttachmentSync())
                        {
                            int bytesToRead = 10000;
                            byte[] buffer = new Byte[bytesToRead];

                            outputFile = Path.Combine(Path.GetTempPath(), downloadAttachmentOutput.FileName);
                            this.Session.LogDebug($"Downloading to {outputFile}");

                            using (BinaryWriter streamWriter = new BinaryWriter(File.Open(outputFile, FileMode.Create, FileAccess.Write)))
                            {
                                int length;
                                do
                                {
                                    length = downloadAttachmentOutput.Stream.Read(buffer, 0, bytesToRead);
                                    streamWriter.Write(buffer, 0, length);
                                    buffer = new Byte[bytesToRead];

                                } while (length > 0);
                            }
                        }


                        string extractionTarget = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                        Directory.CreateDirectory(extractionTarget);

                        this.Session.LogDebug($"Extracting environment contents to {extractionTarget}");

                        ZipFile.ExtractToDirectory(outputFile, extractionTarget);


                        var realTarget = outputPath?.FullName;

                        if (string.IsNullOrEmpty(realTarget))
                        {
                            realTarget = Path.Combine(Directory.GetCurrentDirectory(), "out", environment.Name);
                        }
                        else
                        {
                            realTarget = Path.GetFullPath(realTarget);
                        }

                        Directory.CreateDirectory(realTarget);

                        this.Session.LogDebug($"Moving environment contents from {extractionTarget} to {realTarget}");

                        //Now Create all of the directories
                        foreach (string dirPath in Directory.GetDirectories(extractionTarget, "*",
                            SearchOption.AllDirectories))
                            Directory.CreateDirectory(dirPath.Replace(extractionTarget, realTarget));

                        //Copy all the files & Replaces any files with the same name
                        foreach (string newPath in Directory.GetFiles(extractionTarget, "*.*",
                            SearchOption.AllDirectories))
                            File.Copy(newPath, newPath.Replace(extractionTarget, realTarget), true);



                        this.Session.LogInformation($"Customer environment created at {realTarget}");
                    }

                    break;
                default:
                    break;
            }

            return 0;
        }
    }

}