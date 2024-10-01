using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.MessageBus.Messages;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public class AppInstallationHandler : IAppInstallationHandler
    {
        private readonly ISession _session;

        private readonly ICustomerPortalClient _customerPortalClient;
        
        private readonly IManifestsDownloaderHandler _manifestsDownloaderHandler;

        private bool _isInstallationFinished = false;

        private bool _hasInstallationFailed = false;

        private static DateTime? utcOfLastMessageReceived = null;

        public AppInstallationHandler(ISession session, ICustomerPortalClient customerPortalClient,
                                            IManifestsDownloaderHandler manifestsDownloaderHandler)
        {
            _session = session;
            _customerPortalClient = customerPortalClient;
            _manifestsDownloaderHandler = manifestsDownloaderHandler;
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

        private async Task ProcessAppInstallation(CustomerEnvironmentApplicationPackage customerEnvironmentApplicationPackage, string target, DirectoryInfo outputDir)
        {
            switch (target)
            {
                case "DockerSwarmOnPremisesTarget":
                case "PortainerV2Target":
                case "KubernetesOnPremisesTarget":
                case "OpenShiftOnPremisesTarget":
                    string outputPath = outputDir != null ? outputDir.FullName : Path.Combine(Directory.GetCurrentDirectory(), "out");
                    bool success = await _manifestsDownloaderHandler.Handle(customerEnvironmentApplicationPackage, outputPath);
                    if (success)
                    {
                        _session.LogInformation($"App created at {outputPath}");
                    }
                    break;
                default:
                    break;
            }
        }

        #endregion

        public async Task Handle(string appName, CustomerEnvironmentApplicationPackage customerEnvironmentApplicationPackage, string target, DirectoryInfo outputDir, double? timeoutMinutesMainTask = null, double? timeoutMinutesToGetSomeMBMsg = null)
        {
            // assign the timeout of main task to deploy
            TimeSpan timeoutMainTask = timeoutMinutesMainTask > 0 ? TimeSpan.FromMinutes(timeoutMinutesMainTask.Value) : TimeSpan.FromHours(6); // same timeout as RING (6 hours)

            // assign the timeout of don't receive any message from portal by MB
            TimeSpan timeoutToGetSomeMBMessageTask = timeoutMinutesToGetSomeMBMsg > 0 ? TimeSpan.FromMinutes(timeoutMinutesToGetSomeMBMsg.Value) : TimeSpan.FromMinutes(30);


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

                                throw new TaskCanceledException($"Installation Failed! The installation timed out after {timeoutToGetSomeMBMessageTask.TotalMinutes} minutes because the SDK client did not receive additional expected messages on MessageBus from the portal and the installation is not finished.");
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

            await ProcessAppInstallation(customerEnvironmentApplicationPackage, target, outputDir);
        }
    }
}
