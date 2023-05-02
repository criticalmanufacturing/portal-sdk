using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessOrchestration.EntityTypeManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.EntityTypeManagement.OutputObjects;
using Cmf.LightBusinessObjects.Infrastructure;
using Cmf.MessageBus.Messages;
using Cmf.Services.GenericServiceManagement;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public class AppInstallationHandler : IAppInstallationHandler
    {
        private readonly ISession _session;

        private readonly ICustomerPortalClient _customerPortalClient;

        private bool _isInstallationFinished = false;

        private bool _hasInstallationFailed = false;

        private static DateTime? utcOfLastMessageReceived = null;

        private TimeSpan timeoutMainTask = TimeSpan.FromMinutes(60);

        private TimeSpan timeoutToGetSomeMBMessageTask = TimeSpan.FromMinutes(15);

        public AppInstallationHandler(ISession session, ICustomerPortalClient customerPortalClient)
        {
            _session = session;
            _customerPortalClient = customerPortalClient;
        }

        #region Private Methods

        private void ProcessDeploymentMessage(string subject, MbMessage message)
        {
            // set the DateTime of last message received
            utcOfLastMessageReceived = DateTime.UtcNow;

            if (message != null && !string.IsNullOrWhiteSpace(message.Data))
            {
                var messageContentFormat = new { Data = string.Empty, DeploymentStatus = (AppInstallationStatus?)AppInstallationStatus.NotInstalled, StepId = string.Empty };
                var content = JsonConvert.DeserializeAnonymousType(message.Data, messageContentFormat);

                _session.LogInformation(content.Data);

                if (content.DeploymentStatus == AppInstallationStatus.InstallationFailed || content.DeploymentStatus == AppInstallationStatus.InstallationSucceeded)
                {
                    if (content.DeploymentStatus == AppInstallationStatus.InstallationFailed)
                    {
                        _hasInstallationFailed = true;
                    }
                    _isInstallationFinished = true;
                }
            }
            else
            {
                _session.LogInformation("Unknown message received");
            }
        }

        private async Task ProcessAppInstallation(string appName, CustomerEnvironmentApplicationPackage customerEnvironmentApplicationPackage, string target, DirectoryInfo outputPath)
        {
            switch (target)
            {
                case "DockerSwarmOnPremisesTarget":
                case "PortainerV2Target":
                case "KubernetesOnPremisesTarget":
                case "OpenShiftOnPremisesTarget":
                    // get the attachments of the current customer environment application package
                    GetAttachmentsForEntityInput input = new GetAttachmentsForEntityInput()
                    {
                        Entity = customerEnvironmentApplicationPackage
                    };

                    await Task.Delay(TimeSpan.FromSeconds(1));
                    GetAttachmentsForEntityOutput output = await input.GetAttachmentsForEntityAsync(true);
                    EntityDocumentation attachmentToDownload = null;
                    if (output?.Attachments.Count > 0)
                    {
                        output.Attachments.Sort((a, b) => DateTime.Compare(b.CreatedOn, a.CreatedOn));
                        attachmentToDownload = output.Attachments.Where(x => x.Filename.StartsWith($"App_{appName}")).FirstOrDefault();
                    }

                    if (attachmentToDownload == null)
                    {
                        _session.LogError("No attachment was found to download.");
                    }
                    else
                    {
                        // Download the attachment
                        _session.LogDebug($"Downloading attachment {attachmentToDownload.Filename}");

                        string outputFile = "";
                        using (DownloadAttachmentStreamingOutput downloadAttachmentOutput = await new DownloadAttachmentStreamingInput() { attachmentId = attachmentToDownload.Id }.DownloadAttachmentAsync(true))
                        {
                            int bytesToRead = 10000;
                            byte[] buffer = new byte[bytesToRead];

                            outputFile = Path.Combine(Path.GetTempPath(), downloadAttachmentOutput.FileName);
                            outputFile = outputFile.Replace(" ", "").Replace("\"", "");
                            _session.LogDebug($"Downloading to {outputFile}");

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

                        _session.LogDebug($"Extracting app installation artifact contents to {extractionTarget}");

                        // extract the zip to the previously created dir
                        ZipFile.ExtractToDirectory(outputFile, extractionTarget);

                        // get target full dir
                        string outputPathFullName = outputPath?.FullName;
                        if (string.IsNullOrEmpty(outputPathFullName))
                        {
                            outputPathFullName = Path.Combine(Directory.GetCurrentDirectory(), "out", appName);
                        }
                        else
                        {
                            outputPathFullName = Path.GetFullPath(outputPathFullName);
                        }

                        // ensure the output path exists
                        Directory.CreateDirectory(outputPathFullName);

                        _session.LogDebug($"Moving app installation artifact contents from {extractionTarget} to {outputPathFullName}");

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

                        _session.LogInformation($"App created at {outputPathFullName}");
                    }

                    break;
                default:
                    break;
            }
        }

        #endregion

        public async Task Handle(string appName, CustomerEnvironmentApplicationPackage customerEnvironmentApplicationPackage, string target, DirectoryInfo outputDir, double? timeout = null)
        {
            // assign the timeout of main task to deploy
            if (timeout > 0)
            {
                timeoutMainTask = TimeSpan.FromMinutes(timeout.Value);
            }

            var messageBus = await _customerPortalClient.GetMessageBusTransport();
            var subject = $"CUSTOMERPORTAL.DEPLOYMENT.APP.{customerEnvironmentApplicationPackage.Id}";

            _session.LogDebug($"Subscribing messagebus subject {subject}");
            messageBus.Subscribe(subject, ProcessDeploymentMessage);

            // start deployment
            var startDeploymentInput = new StartDeploymentInput
            {
                CustomerEnvironmentApplicationPackageId = customerEnvironmentApplicationPackage.Id
            };

            _session.LogInformation($"Starting the installation of the App: {appName}...");
            var result = await startDeploymentInput.StartDeploymentAsync(true);

            // show progress from deployment
            using (CancellationTokenSource cancellationTokenMainTask = new CancellationTokenSource(timeoutMainTask))
            {
                // The variable 'utcOfLastMessageReceived' will be set with the UTC of last message received on message bus (on ProcessDeploymentMessage()).
                // This 'cancellationTokenMBMessageReceived' will be restarted if the time past between 'utcOfLastMessageReceived' and the current datetime
                // is less than 'timeoutToGetSomeMBMessageTask'.
                CancellationTokenSource cancellationTokenMBMessageReceived = new CancellationTokenSource(timeoutToGetSomeMBMessageTask);

                // The compositeTokenSource will be a composed token between the cancellationTokenMainTask and the cancellationTokenMBMessageReceived. The first ending returns the exception.
                CancellationTokenSource compositeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenMainTask.Token, cancellationTokenMBMessageReceived.Token);

                while (!this._isInstallationFinished)
                {
                    _session.LogPendingMessages();
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(0.1), compositeTokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        if (cancellationTokenMainTask.Token.IsCancellationRequested)
                        {
                            cancellationTokenMBMessageReceived.Dispose();
                            compositeTokenSource.Dispose();

                            throw new TaskCanceledException($"Installation Failed! The installation timed out after waiting {timeoutMainTask.TotalMinutes} minutes to finish.");
                        }

                        if (cancellationTokenMBMessageReceived.Token.IsCancellationRequested)
                        {
                            if (utcOfLastMessageReceived == null || (DateTime.UtcNow - utcOfLastMessageReceived.Value) >= timeoutToGetSomeMBMessageTask)
                            {
                                compositeTokenSource.Dispose();
                                cancellationTokenMBMessageReceived.Dispose();

                                throw new TaskCanceledException($"Installation Failed! The installation timed out after {timeoutToGetSomeMBMessageTask.TotalMinutes} minutes without messages received on the MessageBus.");
                            }
                            else
                            {
                                cancellationTokenMBMessageReceived.Dispose();
                                compositeTokenSource.Dispose();
                                cancellationTokenMBMessageReceived = new CancellationTokenSource(timeoutToGetSomeMBMessageTask - (DateTime.UtcNow - utcOfLastMessageReceived.Value));
                                compositeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenMainTask.Token, cancellationTokenMBMessageReceived.Token);

                            }
                        }
                    }
                }

                compositeTokenSource.Dispose();
                cancellationTokenMBMessageReceived.Dispose();
            }

            if (_hasInstallationFailed)
            {
                throw new Exception("Installation Failed! Check the logs for more information.");
            }

            await ProcessAppInstallation(appName, customerEnvironmentApplicationPackage, target, outputDir);
        }
    }
}
