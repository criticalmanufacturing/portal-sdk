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
        private readonly string[] _loadingChars = { "|", "/", "-", "\\" };

        private DeploymentProgressTracker _progressTracker;

        public bool HasUninstallationStarted
        {
            get { return _progressTracker?.HasStarted ?? false; }
            private set { /* no-op */ }
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

            // initialize tracker with adapter for app uninstallation
            _progressTracker = new DeploymentProgressTracker(session, new AppUninstallationStatusAdapter());

            messageBus.Subscribe(subject, _progressTracker.ProcessDeploymentMessage);

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

                CancellationTokenSource _cancellationTokenUninstallationQeued = new CancellationTokenSource(timeoutToGetSomeMBMessageTask);
                CancellationTokenSource compositeTokenSourceWithDeploymentQueued = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenMainTask.Token, cancellationTokenMBMessageReceived.Token, _cancellationTokenUninstallationQeued.Token);

                await Task.Run(() => _progressTracker.ShowLoadingIndicator(compositeTokenSourceWithDeploymentQueued.Token));

                while (!this._isUninstallationFinished)
                {
                    if (_progressTracker.HasStarted && _cancellationTokenUninstallationQeued != null && !_cancellationTokenUninstallationQeued.IsCancellationRequested)
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
                            if (DeploymentProgressTracker.UtcOfLastMessageReceived == null || (DateTime.UtcNow - DeploymentProgressTracker.UtcOfLastMessageReceived.Value) >= timeoutToGetSomeMBMessageTask)
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
                                cancellationTokenMBMessageReceived = new CancellationTokenSource(timeoutToGetSomeMBMessageTask - (DateTime.UtcNow - DeploymentProgressTracker.UtcOfLastMessageReceived.Value));
                                compositeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenMainTask.Token, cancellationTokenMBMessageReceived.Token);

                            }
                        }
                    }

                    if (_progressTracker.HasFinished)
                    {
                        if (_progressTracker.HasFailed)
                        {
                            _hasUninstallationFailed = true;
                        }
                        _isUninstallationFinished = true;
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
    }
}
