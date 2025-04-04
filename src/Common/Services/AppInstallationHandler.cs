using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Deployment.Models;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.MessageBus.Messages;
using Newtonsoft.Json;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public class AppInstallationHandler : IAppInstallationHandler
    {
        private readonly ISession _session;
        private readonly ICustomerPortalClient _customerPortalClient;
        private readonly IArtifactsDownloaderHandler _artifactsDownloaderHandler;

        private bool _isInstallationFinished = false;
        private bool _hasInstallationFailed = false;
        private bool _hasInstallationStarted = false;
        private readonly string[] _loadingChars = { "|", "/", "-", "\\" };
        private const string _queuePositionMsg = "Queue Position:";
        private string _pattern = @$"{_queuePositionMsg} (\d+)\n";
        private (int left, int top)? _queuePositionCursorCoordinates = null;
        private (int left, int top) _queuePositionLoadingCursorCoordinates;
        CancellationTokenSource _cancellationTokenDeploymentQueued;
        private bool _presentLoading = false;
        private static DateTime? utcOfLastMessageReceived = null;
        public bool HasInstallationStarted
        {
            get { return _hasInstallationStarted; }
           private set { _hasInstallationStarted = value; }
        }

        public AppInstallationHandler(ISession session, ICustomerPortalClient customerPortalClient,
                                      IArtifactsDownloaderHandler artifactsDownloaderHandler)
        {
            _session = session;
            _customerPortalClient = customerPortalClient;
            _artifactsDownloaderHandler = artifactsDownloaderHandler;
        }

        #region Private Methods
        private async Task ProcessAppInstallation(CustomerEnvironmentApplicationPackage customerEnvironmentApplicationPackage, string target, DirectoryInfo outputDir)
        {
            switch (target)
            {
                case "DockerSwarmOnPremisesTarget":
                case "PortainerV2Target":
                case "KubernetesOnPremisesTarget":
                case "OpenShiftOnPremisesTarget":
                    string outputPath = outputDir != null ? outputDir.FullName : Path.Combine(Directory.GetCurrentDirectory(), "out");
                    bool success = await _artifactsDownloaderHandler.Handle(customerEnvironmentApplicationPackage, outputPath);
                    if (success)
                    {
                        _session.LogInformation($"App created at {outputPath}");
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
            while (!_hasInstallationStarted && !token.IsCancellationRequested)
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
                var messageContentFormat = new { Data = string.Empty, DeploymentStatus = (AppInstallationStatus?)AppInstallationStatus.NotInstalled, StepId = string.Empty };
                var content = JsonConvert.DeserializeAnonymousType(jsonString, messageContentFormat);
                Match match = Regex.Match(content.Data, _pattern);

                msg = content.Data;
                if (match.Success && !_hasInstallationStarted)
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


                if (content.DeploymentStatus == AppInstallationStatus.Installing || content.DeploymentStatus == AppInstallationStatus.Uninstalling)
                {
                    _presentLoading = false;
                    _hasInstallationStarted = true;

                    if (_cancellationTokenDeploymentQueued != null && !_cancellationTokenDeploymentQueued.IsCancellationRequested)
                    {
                        _cancellationTokenDeploymentQueued.Cancel();
                    }
                }


                if (content.DeploymentStatus == AppInstallationStatus.InstallationFailed || content.DeploymentStatus == AppInstallationStatus.InstallationSucceeded)
                {
                    if (content.DeploymentStatus == AppInstallationStatus.InstallationFailed)
                    {
                        _hasInstallationFailed = true;
                    }
                    _isInstallationFinished = true;
                    _hasInstallationStarted = true;
                }
            }
            else
            {
                _session.LogInformation("Unknown message received");
            }
        }


        public async Task Handle(string appName, CustomerEnvironmentApplicationPackage customerEnvironmentApplicationPackage, string target, DirectoryInfo outputDir, double? timeoutMinutesMainTask = null, double? timeoutMinutesToGetSomeMBMsg = null)
        {
            // assign the timeout of main task to deploy
            TimeSpan timeoutMainTask = timeoutMinutesMainTask > 0 ? TimeSpan.FromMinutes(timeoutMinutesMainTask.Value) : TimeSpan.FromHours(6); // same timeout as RING (6 hours)

            // assign the timeout of don't receive any message from portal by MB
            TimeSpan timeoutToGetSomeMBMessageTask = timeoutMinutesToGetSomeMBMsg > 0 ? TimeSpan.FromMinutes(timeoutMinutesToGetSomeMBMsg.Value) : TimeSpan.FromMinutes(30);


            var messageBus = await _customerPortalClient.GetMessageBusTransport();
            var subject = $"CUSTOMERPORTAL.DEPLOYMENT.APP.{customerEnvironmentApplicationPackage.Id}";
            var subjectMessageQueuePosition = $"CUSTOMERPORTAL.DEPLOYMENT.MESSAGEQUEUE.POSITION.{customerEnvironmentApplicationPackage.Id}";

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

                _cancellationTokenDeploymentQueued = new CancellationTokenSource(timeoutToGetSomeMBMessageTask);
                CancellationTokenSource compositeTokenSourceWithDeploymentQueued = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenMainTask.Token, cancellationTokenMBMessageReceived.Token, _cancellationTokenDeploymentQueued.Token);

                await Task.Run(() => ShowLoadingIndicator(compositeTokenSourceWithDeploymentQueued.Token));

                while (!this._isInstallationFinished)
                {
                    if (_hasInstallationStarted && _cancellationTokenDeploymentQueued != null && !_cancellationTokenDeploymentQueued.IsCancellationRequested)
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

                            throw new TaskCanceledException($"Installation Failed! The installation timed out after waiting {timeoutMainTask.TotalMinutes} minutes to finish.");
                        }

                        if (cancellationTokenMBMessageReceived.Token.IsCancellationRequested)
                        {
                            if (utcOfLastMessageReceived == null || (DateTime.UtcNow - utcOfLastMessageReceived.Value) >= timeoutToGetSomeMBMessageTask)
                            {
                                compositeTokenSource?.Dispose();
                                cancellationTokenMBMessageReceived?.Dispose();
                                _cancellationTokenDeploymentQueued?.Dispose();
                                compositeTokenSourceWithDeploymentQueued?.Dispose();

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

                compositeTokenSource?.Dispose();
                cancellationTokenMBMessageReceived?.Dispose();
                _cancellationTokenDeploymentQueued?.Dispose();
                compositeTokenSourceWithDeploymentQueued?.Dispose();
            }

            if (_hasInstallationFailed)
            {
                throw new Exception("Installation Failed! Check the logs for more information.");
            }

            await ProcessAppInstallation(customerEnvironmentApplicationPackage, target, outputDir);
        }
    }
}
