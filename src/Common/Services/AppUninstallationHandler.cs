using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.MessageBus.Messages;
using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public class AppUninstallationHandler(ISession session,
        ICustomerPortalClient customerPortalClient) : AbstractHandler(session, true), IAppUninstallationHandler
    {
        private bool _isUninstallationFinished = false;
        private bool _hasUninstallationFailed = false;
        private bool _hasUninstallationStarted = false;
        private readonly string[] _loadingChars = { "|", "/", "-", "\\" };
        private const string _queuePositionMsg = "Queue Position:";
        private string _pattern = @$"{_queuePositionMsg} (\d+)\\n";
        private (int left, int top)? _queuePositionCursorCoordinates = null;
        private (int left, int top) _queuePositionLoadingCursorCoordinates;
        CancellationTokenSource _cancellationTokenUninstallationQeued;
        private bool _presentLoading = false;
        private static DateTime? utcOfLastMessageReceived = null;

        public bool HasUninstallationStarted
        {
            get { return _hasUninstallationStarted; }
            private set { _hasUninstallationStarted = value; }
        }
        public async Task Handle(CustomerEnvironmentApplicationPackage customerEnvironmentApplicationPackage, double? timeoutMinutesMainTask = null, double? timeoutMinutesToGetSomeMBMsg = null)
        {
            // assign the timeout of main task to deploy
            TimeSpan timeoutMainTask = TimeSpan.FromHours(6); // same timeout as RING (6 hours)

            // assign the timeout of don't receive any message from portal by MB
            TimeSpan timeoutToGetSomeMBMessageTask = TimeSpan.FromMinutes(30);


            var messageBus = await customerPortalClient.GetMessageBusTransport();
            var subject = $"CUSTOMERPORTAL.DEPLOYMENT.APP.{customerEnvironmentApplicationPackage.Id}";

            session.LogDebug($"Subscribing messagebus subject {subject}");
            messageBus.Subscribe(subject, ProcessDeploymentMessage);

            var startAppUninstallInput = new StartAppUninstallInput() { CustomerEnvironmentApplicationPackageId = customerEnvironmentApplicationPackage.Id };
            var result = await startAppUninstallInput.StartAppUninstallAsync();

            // show progress from deployment
            using (CancellationTokenSource cancellationTokenMainTask = new CancellationTokenSource(timeoutMainTask))
            {
                // The variable 'utcOfLastMessageReceived' will be set with the UTC of last message received on message bus (on ProcessDeploymentMessage()).
                // This 'cancellationTokenMBMessageReceived' will be restarted if the time past between 'utcOfLastMessageReceived' and the current datetime
                // is less than 'timeoutToGetSomeMBMessageTask'.
                CancellationTokenSource cancellationTokenMBMessageReceived = new CancellationTokenSource(timeoutToGetSomeMBMessageTask);

                // The compositeTokenSource will be a composed token between the cancellationTokenMainTask and the cancellationTokenMBMessageReceived. The first ending returns the exception.
                CancellationTokenSource compositeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenMainTask.Token, cancellationTokenMBMessageReceived.Token);

                _cancellationTokenUninstallationQeued = new CancellationTokenSource(timeoutToGetSomeMBMessageTask);
                CancellationTokenSource compositeTokenSourceWithDeploymentQueued = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenMainTask.Token, cancellationTokenMBMessageReceived.Token, _cancellationTokenUninstallationQeued.Token);

                await Task.Run(() => ShowLoadingIndicator(compositeTokenSourceWithDeploymentQueued.Token));

                while (!this._isUninstallationFinished)
                {
                    if (_hasUninstallationStarted && _cancellationTokenUninstallationQeued != null && !_cancellationTokenUninstallationQeued.IsCancellationRequested)
                    {
                        _cancellationTokenUninstallationQeued.Cancel();
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
                            _cancellationTokenUninstallationQeued?.Dispose();

                            throw new TaskCanceledException($"Uninstallation Failed! The uninstallation timed out after waiting {timeoutMainTask.TotalMinutes} minutes to finish.");
                        }

                        if (cancellationTokenMBMessageReceived.Token.IsCancellationRequested)
                        {
                            if (utcOfLastMessageReceived == null || (DateTime.UtcNow - utcOfLastMessageReceived.Value) >= timeoutToGetSomeMBMessageTask)
                            {
                                compositeTokenSource?.Dispose();
                                cancellationTokenMBMessageReceived?.Dispose();
                                _cancellationTokenUninstallationQeued?.Dispose();
                                compositeTokenSourceWithDeploymentQueued?.Dispose();

                                throw new TaskCanceledException($"Uninstallation Failed! The uninstallation timed out after {timeoutToGetSomeMBMessageTask.TotalMinutes} minutes because the SDK client did not receive additional expected messages on MessageBus from the portal and the installation is not finished.");
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
                _cancellationTokenUninstallationQeued?.Dispose();
                compositeTokenSourceWithDeploymentQueued?.Dispose();
            }

            if (_hasUninstallationFailed)
            {
                throw new Exception("Uninstallation Failed! Check the logs for more information.");
            }
        }
        public void ProcessDeploymentMessage(string subject, MbMessage message)
        {
            int initialTopLine;
            string msg;
            // set the DateTime of last message received
            utcOfLastMessageReceived = DateTime.UtcNow;

            if (message != null && !string.IsNullOrWhiteSpace(message.Data))
            {
                // handle escape
                string jsonString = message.Data.Trim('\"');
                jsonString = jsonString.Replace("\\\"", "\"");
                var messageContentFormat = new { Data = string.Empty, DeploymentStatus = (AppInstallationStatus?)AppInstallationStatus.NotInstalled, StepId = string.Empty };
                var content = JsonConvert.DeserializeAnonymousType(jsonString, messageContentFormat);
                msg = content.Data ?? string.Empty;
                Match match = Regex.Match(msg, _pattern);

                if (match.Success && !_hasUninstallationStarted)
                {
                    string position = match.Groups[1].Value;

                    if (!string.IsNullOrWhiteSpace(position))
                    {
                        if (_queuePositionCursorCoordinates == null)
                        {
                            _queuePositionCursorCoordinates = Console.GetCursorPosition();
                        }

                        initialTopLine = Console.CursorTop;
                        msg = $"{_queuePositionMsg} {position}";
                        Console.SetCursorPosition(_queuePositionCursorCoordinates.Value.left, _queuePositionCursorCoordinates.Value.top - 1);
                        session.LogInformation(msg);

                        _queuePositionLoadingCursorCoordinates = (_queuePositionCursorCoordinates.Value.left + msg.Length, _queuePositionCursorCoordinates.Value.top - 1);
                        Console.SetCursorPosition(0, initialTopLine);

                        _presentLoading = true;
                    }
                }
                if (!msg.StartsWith(_queuePositionMsg))
                {
                    session.LogInformation(msg);
                }


                if (content.DeploymentStatus == AppInstallationStatus.Uninstalling)
                {
                    _presentLoading = false;
                    _hasUninstallationStarted = true;

                    if (_cancellationTokenUninstallationQeued != null && !_cancellationTokenUninstallationQeued.IsCancellationRequested)
                    {
                        _cancellationTokenUninstallationQeued.Cancel();
                    }
                }


                if (content.DeploymentStatus == AppInstallationStatus.UninstallationFailed || content.DeploymentStatus == AppInstallationStatus.UninstallationSucceeded)
                {
                    if (content.DeploymentStatus == AppInstallationStatus.UninstallationFailed)
                    {
                        _hasUninstallationFailed = true;
                    }
                    _isUninstallationFinished = true;
                    _hasUninstallationStarted = true;
                }
            }
            else
            {
                session.LogInformation("Unknown message received");
            }
        }
        private async Task ShowLoadingIndicator(CancellationToken token)
        {
            int loadingIndex = 0;
            (int left, int top) initialPosition;
            while (!_hasUninstallationFailed && !token.IsCancellationRequested)
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
    }
}
