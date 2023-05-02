﻿using Cmf.CustomerPortal.BusinessObjects;
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
    public class EnvironmentDeploymentHandler : IEnvironmentDeploymentHandler
    {
        private readonly ISession _session;
        private readonly ICustomerPortalClient _customerPortalClient;
        private bool _isDeploymentFinished = false;
        private bool _hasDeploymentFailed = false;

        private static DateTime? utcOfLastMessageReceived = null;

        private TimeSpan timeoutMainTask = TimeSpan.FromMinutes(60);
        private TimeSpan timeoutToGetSomeMBMessageTask = TimeSpan.FromMinutes(15);

        public EnvironmentDeploymentHandler(ISession session, ICustomerPortalClient customerPortalClient)
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
                var messageContentFormat = new { Data = string.Empty, DeploymentStatus = (DeploymentStatus?)DeploymentStatus.NotDeployed, StepId = string.Empty };
                var content = JsonConvert.DeserializeAnonymousType(message.Data, messageContentFormat);

                _session.LogInformation(content.Data);

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
                _session.LogInformation("Unknown message received");
            }
        }

        private async Task ProcessEnvironmentDeployment(CustomerEnvironment environment, DeploymentTarget target, DirectoryInfo outputPath)
        {
            switch (target)
            {
                case DeploymentTarget.dockerswarm:
                case DeploymentTarget.KubernetesOnPremisesTarget:
                case DeploymentTarget.OpenShiftOnPremisesTarget:

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

                        _session.LogDebug($"Extracting environment contents to {extractionTarget}");

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

                        _session.LogDebug($"Moving environment contents from {extractionTarget} to {outputPathFullName}");

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

                        _session.LogInformation($"Customer environment created at {outputPathFullName}");
                    }

                    break;
                default:
                    break;
            }
        }

        #endregion

        public async Task Handle(bool interactive, CustomerEnvironment customerEnvironment, DeploymentTarget deploymentTarget, DirectoryInfo outputDir, double? minutesTimeoutMainTask = null)
        {
            // assign the timeout of main task to deploy
            if (minutesTimeoutMainTask > 0)
            {
                timeoutMainTask = TimeSpan.FromMinutes(minutesTimeoutMainTask.Value);
            }

            var messageBus = await _customerPortalClient.GetMessageBusTransport();
            var subject = $"CUSTOMERPORTAL.DEPLOYMENT.{customerEnvironment.Id}";

            _session.LogDebug($"Subscribing messagebus subject {subject}");
            messageBus.Subscribe(subject, ProcessDeploymentMessage);

            // start deployment
            var startDeploymentInput = new StartDeploymentInput
            {
                CustomerEnvironment = customerEnvironment
            };

            if (interactive)
            {
                string infrastructureUrl = $"{(ClientConfigurationProvider.ClientConfiguration.UseSsl ? "https" : "http")}://{ClientConfigurationProvider.ClientConfiguration.HostAddress}/Entity/CustomerEnvironment/{customerEnvironment.Id}/View/Installation";

                _session.LogInformation($"Environment {customerEnvironment.Name} was created, please access the portal url to configure the environment and start the installation:");
                _session.LogInformation(infrastructureUrl);
                _session.LogInformation($"Waiting for user configuration...");
            }
            else
            {
                _session.LogInformation($"Starting deployment of environment {customerEnvironment.Name}...");
                var result = await startDeploymentInput.StartDeploymentAsync(true);
            }


            using (CancellationTokenSource cancellationTokenMainTask = new CancellationTokenSource(timeoutMainTask))
            {
                // The variable 'utcOfLastMessageReceived' will be set with the UTC of last message received on message bus (on ProcessDeploymentMessage()).
                // This 'cancellationTokenMBMessageReceived' will be restarted if the time past between 'utcOfLastMessageReceived' and the current datetime
                // is less than 'timeoutToGetSomeMBMessageTask'.
                CancellationTokenSource cancellationTokenMBMessageReceived = new CancellationTokenSource(timeoutToGetSomeMBMessageTask);

                // The compositeTokenSource will be a composed token between the cancellationTokenMainTask and the cancellationTokenMBMessageReceived. The first ending returns the exception.
                CancellationTokenSource compositeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenMainTask.Token, cancellationTokenMBMessageReceived.Token);

                while (!this._isDeploymentFinished)
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

                            throw new TaskCanceledException($"Deployment Failed! The deployment timed out after {timeoutMainTask.TotalMinutes} minutes waiting for deployment to be finished.");
                        }

                        if (cancellationTokenMBMessageReceived.Token.IsCancellationRequested)
                        {
                            if (utcOfLastMessageReceived == null || (DateTime.UtcNow - utcOfLastMessageReceived.Value) >= timeoutToGetSomeMBMessageTask)
                            {
                                compositeTokenSource.Dispose();
                                cancellationTokenMBMessageReceived.Dispose();

                                throw new TaskCanceledException($"Deployment Failed! The deployment timed out after {timeoutToGetSomeMBMessageTask.TotalMinutes} minutes without messages received on MessageBus and waiting for deployment to be finished.");
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
            if (_hasDeploymentFailed)
            {
                throw new Exception("Deployment Failed! Check the logs for more information");
            }

            await ProcessEnvironmentDeployment(customerEnvironment, deploymentTarget, outputDir);
        }

        public async Task WaitForEnvironmentsToBeTerminated(CustomerEnvironmentCollection customerEnvironments)
        {
            var timeout = new TimeSpan(0, 30, 0);
            var waitPeriod = new TimeSpan(0, 0, 5);
            var ids = new HashSet<long>(customerEnvironments.Select(ce => ce.Id));
            var ctSource = new CancellationTokenSource();
            CancellationToken ct = ctSource.Token;
            var task = Task.Run(async () =>
            {
                var result = false;
                while (!result)
                {
                    if (ct.IsCancellationRequested)
                    {
                        ct.ThrowIfCancellationRequested();
                    }

                    var checkedCustomerEnvironments = await _customerPortalClient.GetCustomerEnvironmentsById(ids.ToArray());
                    foreach (var ce in checkedCustomerEnvironments)
                    {
                        // keep track of which environments have already been terminated
                        if ((ce.Status == DeploymentStatus.Terminated || ce.Status == DeploymentStatus.TerminationFailed) &&
                        ce.UniversalState == Foundation.Common.Base.UniversalState.Terminated)
                        {
                            ids.Remove(ce.Id);
                        }
                    }

                    if (ids.Count == 0)
                    {
                        result = true;
                    }

                    await Task.Delay(waitPeriod);
                }
            });

            try
            {
                if (task != await Task.WhenAny(task, Task.Delay(timeout)))
                {
                    throw new TimeoutException($"Timeout while waiting for Customer Environment {customerEnvironments.First().Name} versions to be terminated.");
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                ctSource.Cancel();
                ctSource.Dispose();
            }
        }
    }
}
