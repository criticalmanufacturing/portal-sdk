using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Deployment.Models;
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
        private readonly string[] _loadingChars = { "|", "/", "-", "\\" };
        private const string _queuePositionMsg = "Queue Position:";
        private string _pattern = @$"{_queuePositionMsg} (\d+)\n";
        private (int left, int top)? _queuePositionCursorCoordinates = null;
        private (int left, int top) _queuePositionLoadingCursorCoordinates;
        CancellationTokenSource _cancellationTokenDeploymentQueued;
        private bool _presentLoading = false;
        private static DateTime? utcOfLastMessageReceived = null;

        public bool HasDeploymentStarted 
        {
            get { return _hasDeploymentStarted; }
            private set { _hasDeploymentStarted = value; }
        }


        public EnvironmentDeploymentHandler(ISession session, ICustomerPortalClient customerPortalClient,
                                            IArtifactsDownloaderHandler artifactsDownloaderHandler)
        {
            _session = session;
            _customerPortalClient = customerPortalClient;
            _artifactsDownloaderHandler = artifactsDownloaderHandler;
        }

        #region Private Methods
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

        private async Task ShowLoadingIndicator(CancellationToken token)
        {
            int loadingIndex = 0;
            (int left, int top) initialPosition;
            while (!_hasDeploymentStarted && !token.IsCancellationRequested)
            {
                try
                {
                    if (_presentLoading)
                    {
                        initialPosition = Console.GetCursorPosition();
                        Console.SetCursorPosition(_queuePositionLoadingCursorCoordinates.left, _queuePositionLoadingCursorCoordinates.top);
                        Console.Write($" {_loadingChars[loadingIndex]} {new string(' ', Console.WindowWidth)}");
                        loadingIndex = (loadingIndex + 1) % _loadingChars.Length;
                        Console.SetCursorPosition(initialPosition.left, initialPosition.top);
                    }
                }
                catch { }

                await Task.Delay(500);
            }
        }

        #endregion
        public void ProcessDeploymentMessage(string subject, MbMessage message)
        {
            string position;
            int initialTopLine;
            string msg;
            // set the DateTime of last message received
            utcOfLastMessageReceived = DateTime.UtcNow;
            if (message != null && !string.IsNullOrWhiteSpace(message.Data))
            {
                // handle escape
                string jsonString = message.Data.Trim('\"');
                jsonString = jsonString.Replace("\\\"", "\"").Replace("\\\\", "\\");
                var messageContentFormat = new { Data = string.Empty, DeploymentStatus = (DeploymentStatus?)DeploymentStatus.NotDeployed, StepId = string.Empty };
                var content = JsonConvert.DeserializeAnonymousType(jsonString, messageContentFormat);
                Match match = Regex.Match(content.Data, _pattern);
                msg = content.Data;
                if (match.Success && !_hasDeploymentStarted)
                {
                    position = match.Groups[1].Value;

                    if (!string.IsNullOrWhiteSpace(position))
                    {
                        if (_queuePositionCursorCoordinates == null)
                        {
                            _queuePositionCursorCoordinates = Console.GetCursorPosition();
                        }

                        initialTopLine = Console.CursorTop;
                        msg = $"{_queuePositionMsg} {position}";
                        Console.SetCursorPosition(_queuePositionCursorCoordinates.Value.left, _queuePositionCursorCoordinates.Value.top - 1);
                        _session.LogInformation(msg);
                        _queuePositionLoadingCursorCoordinates = (_queuePositionCursorCoordinates.Value.left + msg.Length, _queuePositionCursorCoordinates.Value.top - 1);
                        Console.SetCursorPosition(0, initialTopLine);
                        _presentLoading = true;
                    }
                }

                if (!msg.StartsWith(_queuePositionMsg))
                {
                    _session.LogInformation(msg);
                }

                if (content.DeploymentStatus == DeploymentStatus.Deploying || content.DeploymentStatus == DeploymentStatus.Terminating)
                {
                    _presentLoading = false;
                    _hasDeploymentStarted = true;

                    if (_cancellationTokenDeploymentQueued != null && !_cancellationTokenDeploymentQueued.IsCancellationRequested)
                    {
                        _cancellationTokenDeploymentQueued.Cancel();
                    }
                }

                if (content.DeploymentStatus == DeploymentStatus.DeploymentFailed || content.DeploymentStatus == DeploymentStatus.DeploymentPartiallySucceeded || content.DeploymentStatus == DeploymentStatus.DeploymentSucceeded)
                {
                    if (content.DeploymentStatus == DeploymentStatus.DeploymentFailed)
                    {
                        _hasDeploymentFailed = true;
                    }
                    _isDeploymentFinished = true;
                    _hasDeploymentStarted = true;
                }
            }
            else
            {
                _session.LogInformation("Unknown message received");
            }
        }

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

                _cancellationTokenDeploymentQueued = new CancellationTokenSource(timeoutToGetSomeMBMessageTask);
                CancellationTokenSource compositeTokenSourceWithDeploymentQueued = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenMainTask.Token, cancellationTokenMBMessageReceived.Token, _cancellationTokenDeploymentQueued.Token);

                await Task.Run(() => ShowLoadingIndicator(compositeTokenSourceWithDeploymentQueued.Token));

                while (!this._isDeploymentFinished)
                {
                    if (_hasDeploymentStarted && _cancellationTokenDeploymentQueued != null && !_cancellationTokenDeploymentQueued.IsCancellationRequested)
                    {
                        _cancellationTokenDeploymentQueued.Cancel();
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
                            _cancellationTokenDeploymentQueued?.Dispose();

                            throw new TaskCanceledException($"Deployment Failed! The deployment timed out after {timeoutMainTask.TotalMinutes} minutes waiting for deployment to be finished.");
                        }

                        if (cancellationTokenMBMessageReceived.Token.IsCancellationRequested)
                        {
                            if (utcOfLastMessageReceived == null || (DateTime.UtcNow - utcOfLastMessageReceived.Value) >= timeoutToGetSomeMBMessageTask)
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
                                cancellationTokenMBMessageReceived = new CancellationTokenSource(timeoutToGetSomeMBMessageTask - (DateTime.UtcNow - utcOfLastMessageReceived.Value));
                                compositeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenMainTask.Token, cancellationTokenMBMessageReceived.Token);
                            }
                        }
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
