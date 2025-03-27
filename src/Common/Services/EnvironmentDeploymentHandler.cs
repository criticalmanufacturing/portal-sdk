using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.LightBusinessObjects.Infrastructure;
using Cmf.MessageBus.Messages;
using Newtonsoft.Json;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public class EnvironmentDeploymentHandler : IEnvironmentDeploymentHandler
    {
        private readonly ISession _session;
        private readonly ICustomerPortalClient _customerPortalClient;
        private readonly IArtifactsDownloaderHandler _artifactsDownloaderHandler;
        private bool _isDeploymentFinished = false;
        private bool _hasDeploymentFailed = false;
        private bool _hasDeploymentStarted = false;
        private readonly string[] loadingChars = { "|", "/", "-", "\\" };
        private const string queuePositionMsg = "Queue Position:";
        string pattern = @$"{queuePositionMsg} \d+\n";
        private (int left, int top)? queuePositionCursorCoordinates = null;
        CancellationTokenSource cancellationTokenDeploymentQueued;
        static SemaphoreSlim semaphore = new(1);

        private static DateTime? utcOfLastMessageReceived = null;

        public EnvironmentDeploymentHandler(ISession session, ICustomerPortalClient customerPortalClient,
                                            IArtifactsDownloaderHandler artifactsDownloaderHandler)
        {
            _session = session;
            _customerPortalClient = customerPortalClient;
            _artifactsDownloaderHandler = artifactsDownloaderHandler;
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

                if (!_hasDeploymentStarted && (content.DeploymentStatus != null && content.DeploymentStatus != DeploymentStatus.QueuedDeployment && content.DeploymentStatus != DeploymentStatus.QueuedTermination && content.DeploymentStatus != DeploymentStatus.NotDeployed))
                {
                    _hasDeploymentStarted = true;
                    if (cancellationTokenDeploymentQueued != null && !cancellationTokenDeploymentQueued.IsCancellationRequested)
                    {
                        cancellationTokenDeploymentQueued.Cancel();
                    }
                }

                _session.LogInformation(content.Data);

                // handle the start position of queue position on console
                if (!_hasDeploymentStarted && !string.IsNullOrWhiteSpace(content.Data)
                    && queuePositionCursorCoordinates == null && Regex.IsMatch(content.Data, pattern))
                {
                    queuePositionCursorCoordinates = Console.GetCursorPosition();

                    // update for the correct row (remove 1 from top coordinate because \n)
                    queuePositionCursorCoordinates = (queuePositionCursorCoordinates.Value.left, queuePositionCursorCoordinates.Value.top - 1);
                }

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

        private async Task ProcessEnvironmentDeployment(CustomerEnvironment environment, DeploymentTarget target, DirectoryInfo outputDir)
        {
            switch (target)
            {
                case DeploymentTarget.dockerswarm:
                case DeploymentTarget.KubernetesOnPremisesTarget:
                case DeploymentTarget.OpenShiftOnPremisesTarget:
                    string outputPath = outputDir != null ? outputDir.FullName : Path.Combine(Directory.GetCurrentDirectory(), "out");
                    bool success = await _artifactsDownloaderHandler.Handle(environment, outputPath);
                    if (success)
                    {
                        _session.LogInformation($"Customer Environment created at {outputPath}");
                    }
                    break;
                default:
                    break;
            }
        }

        private async Task UpdateQueuePositionAsync(long deployableId, CancellationToken token)
        {
            bool presentingLoading = false;
            int? position = null;
            int initialTopLine;
            string msg;

            while (!this._isDeploymentFinished && !_hasDeploymentStarted && !token.IsCancellationRequested)
            {
                try
                {
                    position = new GetMessagePositionInDeploymentQueueInput()
                    {
                        DeployableId = deployableId
                    }.GetMessagePositionInDeploymentQueueSync().MessageQueuePosition;

                    await semaphore.WaitAsync(token);

                    if (position != null && queuePositionCursorCoordinates != null)
                    {
                        initialTopLine = Console.CursorTop;
                        msg = $"{queuePositionMsg} {position}";
                        Console.SetCursorPosition(queuePositionCursorCoordinates.Value.left, queuePositionCursorCoordinates.Value.top - 1);
                        _session.LogInformation(msg);

                        var cursorQueuePosition = (queuePositionCursorCoordinates.Value.left + msg.Length + 1, queuePositionCursorCoordinates.Value.top - 1);

                        if (!presentingLoading)
                        {
                            presentingLoading = true;
                            _ = Task.Run(() => ShowLoadingIndicator(cursorQueuePosition, token));
                        }
                        Console.SetCursorPosition(0, initialTopLine);
                    }
                }
                catch { }
                finally
                {
                    semaphore.Release();
                }

                await Task.Delay(5000);
            }
        }

        private async Task ShowLoadingIndicator((int Left, int Top) cursorPosition, CancellationToken token)
        {
            int loadingIndex = 0;
            (int Left, int Top) initialPosition;
            while (!this._isDeploymentFinished && !_hasDeploymentStarted && !token.IsCancellationRequested)
            {
                try
                {
                    await semaphore.WaitAsync(token);
                    initialPosition = Console.GetCursorPosition();
                    Console.SetCursorPosition(cursorPosition.Left, cursorPosition.Top);
                    Console.Write($"{loadingChars[loadingIndex]}");
                    loadingIndex = (loadingIndex + 1) % loadingChars.Length;
                    Console.SetCursorPosition(initialPosition.Left, initialPosition.Top);
                }
                catch { }
                finally
                {
                    semaphore.Release();
                }
                await Task.Delay(500);
            }
        }

        #endregion

        public async Task Handle(bool interactive, CustomerEnvironment customerEnvironment, DeploymentTarget deploymentTarget, DirectoryInfo outputDir, double? minutesTimeoutMainTask, double? minutesTimeoutToGetSomeMBMsg = null)
        {
            // assign the timeout of main task to deploy
            TimeSpan timeoutMainTask = minutesTimeoutMainTask > 0 ? TimeSpan.FromMinutes(minutesTimeoutMainTask.Value) : TimeSpan.FromHours(6); // same timeout as RING (6 hours)


            // assign the timeout of don't receive any message from portal by MB
            TimeSpan timeoutToGetSomeMBMessageTask = minutesTimeoutToGetSomeMBMsg > 0 ? TimeSpan.FromMinutes(minutesTimeoutToGetSomeMBMsg.Value) : TimeSpan.FromMinutes(30);


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

                cancellationTokenDeploymentQueued = new CancellationTokenSource(timeoutToGetSomeMBMessageTask);
                CancellationTokenSource compositeTokenSourceWithDeploymentQueued = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenMainTask.Token, cancellationTokenMBMessageReceived.Token, cancellationTokenDeploymentQueued.Token);

                _ = Task.Run(() => UpdateQueuePositionAsync(customerEnvironment.Id, compositeTokenSourceWithDeploymentQueued.Token));

                while (!this._isDeploymentFinished)
                {
                    if (_hasDeploymentStarted && cancellationTokenDeploymentQueued != null && !cancellationTokenDeploymentQueued.IsCancellationRequested)
                    {
                        cancellationTokenDeploymentQueued.Cancel();
                        compositeTokenSourceWithDeploymentQueued.Dispose();
                    }

                    _session.LogPendingMessages();

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
                            cancellationTokenDeploymentQueued?.Dispose();

                            throw new TaskCanceledException($"Deployment Failed! The deployment timed out after {timeoutMainTask.TotalMinutes} minutes waiting for deployment to be finished.");
                        }

                        if (cancellationTokenMBMessageReceived.Token.IsCancellationRequested)
                        {
                            if (utcOfLastMessageReceived == null || (DateTime.UtcNow - utcOfLastMessageReceived.Value) >= timeoutToGetSomeMBMessageTask)
                            {
                                compositeTokenSource?.Dispose();
                                cancellationTokenMBMessageReceived?.Dispose();
                                cancellationTokenDeploymentQueued?.Dispose();
                                compositeTokenSourceWithDeploymentQueued?.Dispose();

                                throw new TaskCanceledException($"Deployment Failed! The deployment timed out after {timeoutToGetSomeMBMessageTask.TotalMinutes} minutes because the SDK client did not receive additional expected messages on MessageBus from the portal and the installation is not finished.");
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
                compositeTokenSource?.Dispose();
                cancellationTokenMBMessageReceived?.Dispose();
                cancellationTokenDeploymentQueued?.Dispose();
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

                    var checkedCustomerEnvironments = await _customerPortalClient.GetCustomerEnvironmentsById(ids.ToArray());
                    foreach (var ce in checkedCustomerEnvironments)
                    {
                        // keep track of which environments have already been terminated
                        if ((ce.Status == DeploymentStatus.Terminated || ce.Status == DeploymentStatus.TerminationFailed) &&
                        ce.UniversalState == Foundation.Common.Base.UniversalState.Terminated)
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
