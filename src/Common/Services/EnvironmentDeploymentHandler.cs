using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.Foundation.Common.Base;
using Cmf.LightBusinessObjects.Infrastructure;
using Cmf.MessageBus.Messages;
using Newtonsoft.Json;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public class EnvironmentDeploymentHandler(
        ISession session,
        IFileSystem fileSystem,
        ICustomerPortalClient customerPortalClient,
        IArtifactsDownloaderHandler artifactsDownloaderHandler)
        : IEnvironmentDeploymentHandler
    {
        private bool _isDeploymentFinished = false;
        private bool _hasDeploymentFailed = false;
        private readonly string[] _loadingChars = { "|", "/", "-", "\\" };

        private DeploymentProgressTracker _progressTracker;

        public bool HasDeploymentStarted 
        {
            get { return _progressTracker?.HasStarted ?? false; }
            private set { /* kept for compatibility - no-op */ }
        }


        #region Private Methods
        private async Task ProcessEnvironmentDeployment(CustomerEnvironment environment, DeploymentTarget target, DirectoryInfo outputDir)
        {
            switch (target)
            {
                case DeploymentTarget.dockerswarm:
                case DeploymentTarget.KubernetesOnPremisesTarget:
                case DeploymentTarget.OpenShiftOnPremisesTarget:
                    string outputPath = outputDir != null ? outputDir.FullName : fileSystem.Path.Combine(fileSystem.Directory.GetCurrentDirectory(), "out");
                    bool success = await artifactsDownloaderHandler.Handle(environment, outputPath);
                    if (success)
                    {
                        session.LogInformation($"Customer Environment created at {outputPath}");
                    }
                    break;
                default:
                    break;
            }
        }

        #endregion
        public async Task Handle(bool interactive, CustomerEnvironment customerEnvironment, DeploymentTarget deploymentTarget, DirectoryInfo outputDir, double? minutesTimeoutMainTask, double? minutesTimeoutToGetSomeMBMsg = null)
        {
            // assign the timeout of main task to deploy
            TimeSpan timeoutMainTask = minutesTimeoutMainTask > 0 ? TimeSpan.FromMinutes(minutesTimeoutMainTask.Value) : TimeSpan.FromHours(6); // same timeout as RING (6 hours)


            // assign the timeout of don't receive any message from portal by MB
            TimeSpan timeoutToGetSomeMBMessageTask = minutesTimeoutToGetSomeMBMsg > 0 ? TimeSpan.FromMinutes(minutesTimeoutToGetSomeMBMsg.Value) : TimeSpan.FromMinutes(30);

            var messageBus = await customerPortalClient.GetMessageBusTransport();
            var subject = $"CUSTOMERPORTAL.DEPLOYMENT.{customerEnvironment.Id}";

            session.LogDebug($"Subscribing messagebus subject {subject}");

            // initialize tracker with adapter for environment
            _progressTracker = new DeploymentProgressTracker(session, new EnvironmentDeploymentStatusAdapter());

            messageBus.Subscribe(subject, _progressTracker.ProcessDeploymentMessage);

            // start deployment
            var startDeploymentInput = new StartDeploymentInput
            {
                CustomerEnvironment = customerEnvironment
            };

            if (interactive)
            {
                string infrastructureUrl = $"{(ClientConfigurationProvider.ClientConfiguration.UseSsl ? "https" : "http")}://{ClientConfigurationProvider.ClientConfiguration.HostAddress}/Entity/CustomerEnvironment/{customerEnvironment.Id}/View/Installation";

                session.LogInformation($"Environment {customerEnvironment.Name} was created, please access the portal url to configure the environment and start the installation:");
                session.LogInformation(infrastructureUrl);
                session.LogInformation($"Waiting for user configuration...");
            }
            else
            {
                session.LogInformation($"Starting deployment of environment {customerEnvironment.Name}...");
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

                CancellationTokenSource _cancellationTokenDeploymentQueued = new CancellationTokenSource(timeoutToGetSomeMBMessageTask);
                CancellationTokenSource compositeTokenSourceWithDeploymentQueued = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenMainTask.Token, cancellationTokenMBMessageReceived.Token, _cancellationTokenDeploymentQueued.Token);

                await Task.Run(() => _progressTracker.ShowLoadingIndicator(compositeTokenSourceWithDeploymentQueued.Token));

                while (!this._isDeploymentFinished)
                {
                    if (_progressTracker.HasStarted && _cancellationTokenDeploymentQueued != null && !_cancellationTokenDeploymentQueued.IsCancellationRequested)
                    {
                        _cancellationTokenDeploymentQueued.Cancel();
                        compositeTokenSourceWithDeploymentQueued.Dispose();
                    }

                    session.LogPendingMessages();

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(0.1), compositeTokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        if (cancellationTokenMainTask.Token.IsCancellationRequested)
                        {
                            cancellationTokenMBMessageReceived?.Dispose();
                            compositeTokenSource?.Dispose();
                            compositeTokenSourceWithDeploymentQueued?.Dispose();
                            _cancellationTokenDeploymentQueued?.Dispose();

                            throw new TaskCanceledException($"Deployment Failed! The deployment timed out after {timeoutMainTask.TotalMinutes} minutes waiting for deployment to be finished.");
                        }

                        if (cancellationTokenMBMessageReceived.Token.IsCancellationRequested)
                        {
                            if (DeploymentProgressTracker.UtcOfLastMessageReceived == null || (DateTime.UtcNow - DeploymentProgressTracker.UtcOfLastMessageReceived.Value) >= timeoutToGetSomeMBMessageTask)
                            {
                                compositeTokenSource?.Dispose();
                                cancellationTokenMBMessageReceived?.Dispose();
                                _cancellationTokenDeploymentQueued?.Dispose();
                                compositeTokenSourceWithDeploymentQueued?.Dispose();

                                throw new TaskCanceledException($"Deployment Failed! The deployment timed out after {timeoutToGetSomeMBMessageTask.TotalMinutes} minutes because the SDK client did not receive additional expected messages on MessageBus from the portal and the installation is not finished.");
                            }
                            else
                            {
                                cancellationTokenMBMessageReceived.Dispose();
                                compositeTokenSource.Dispose();
                                cancellationTokenMBMessageReceived = new CancellationTokenSource(timeoutToGetSomeMBMessageTask - (DateTime.UtcNow - DeploymentProgressTracker.UtcOfLastMessageReceived.Value));
                                compositeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenMainTask.Token, cancellationTokenMBMessageReceived.Token);
                            }
                        }
                    }

                    if (_progressTracker.HasFinished)
                    {
                        if (_progressTracker.HasFailed)
                        {
                            _hasDeploymentFailed = true;
                        }
                        _isDeploymentFinished = true;
                    }
                }
                compositeTokenSource?.Dispose();
                cancellationTokenMBMessageReceived?.Dispose();
                _cancellationTokenDeploymentQueued?.Dispose();
                compositeTokenSourceWithDeploymentQueued?.Dispose();
            }
            if (_hasDeploymentFailed)
            {
                throw new Exception("Deployment Failed! Check the logs for more information");
            }

            await ProcessEnvironmentDeployment(customerEnvironment, deploymentTarget, outputDir);
        }

        public async Task<List<long>> WaitForEnvironmentsToBeTerminated(CustomerEnvironmentCollection customerEnvironments)
        {
            var timeout = new TimeSpan(0, 30, 0);
            var waitPeriod = new TimeSpan(0, 0, 5);
            var ids = new HashSet<long>(customerEnvironments.Select(ce => ce.Id));
            var ctSource = new CancellationTokenSource();
            CancellationToken ct = ctSource.Token;
            List<long> ceTerminationFailedIds = new List<long>();
            var task = Task.Run(async () =>
            {
                var result = false;
                while (!result)
                {
                    if (ct.IsCancellationRequested)
                    {
                        ct.ThrowIfCancellationRequested();
                    }

                    var checkedCustomerEnvironments = await customerPortalClient.GetCustomerEnvironmentsById(ids.ToArray());
                    foreach (var ce in checkedCustomerEnvironments)
                    {
                        // keep track of which environments have already been terminated
                        if ((ce.Status == DeploymentStatus.Terminated || ce.Status == DeploymentStatus.TerminationFailed) &&
                        ce.UniversalState == UniversalState.Terminated)
                        {
                            ids.Remove(ce.Id);

                            if (ce.Status == DeploymentStatus.TerminationFailed)
                            {
                                ceTerminationFailedIds.Add(ce.Id);
                            }
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
                return ceTerminationFailedIds;
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
