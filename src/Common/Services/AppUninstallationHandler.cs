using Cmf.CustomerPortal.BusinessObjects;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public class AppUninstallationHandler(ISession session,
        ICustomerPortalClient customerPortalClient) : AbstractHandler(session, true), IAppUninstallationHandler
    {
        private bool _isUninstallationFinished = false;
        private bool _hasUninstallationFailed = false;

        private DeploymentProgressTracker<AppInstallationStatus> _progressTracker;
        public async Task Handle(CustomerEnvironmentApplicationPackage customerEnvironmentApplicationPackage, bool removeDeployments = false, bool removeVolumes = false, double? timeoutMinutesMainTask = null, double? timeoutMinutesToGetSomeMBMsg = null)
        {
              TimeSpan timeoutMainTask = timeoutMinutesMainTask > 0 ? TimeSpan.FromMinutes(timeoutMinutesMainTask.Value) : TimeSpan.FromHours(6); // same timeout as RING (6 hours)

            // assign the timeout of don't receive any message from portal by MB
            TimeSpan timeoutToGetSomeMBMessageTask = timeoutMinutesToGetSomeMBMsg > 0 ? TimeSpan.FromMinutes(timeoutMinutesToGetSomeMBMsg.Value) : TimeSpan.FromMinutes(30);

            var messageBus = await customerPortalClient.GetMessageBusTransport();
            var subject = $"CUSTOMERPORTAL.DEPLOYMENT.APP.{customerEnvironmentApplicationPackage.Id}";

            session.LogDebug($"Subscribing messagebus subject {subject}");

            // initialize tracker with adapter for app uninstallation
            _progressTracker = new DeploymentProgressTracker<AppInstallationStatus>(session, new AppUninstallationStatusAdapter());

            messageBus.Subscribe(subject, _progressTracker.ProcessDeploymentMessage);

            await customerPortalClient.StartAppUninstall(customerEnvironmentApplicationPackage.Id, removeDeployments, removeVolumes);

            // show progress from deployment
            using (CancellationTokenSource cancellationTokenMainTask = new CancellationTokenSource(timeoutMainTask))
            {
                // The variable 'utcOfLastMessageReceived' will be set with the UTC of last message received on message bus (on ProcessDeploymentMessage()).
                // This 'cancellationTokenMBMessageReceived' will be restarted if the time past between 'utcOfLastMessageReceived' and the current datetime
                // is less than 'timeoutToGetSomeMBMessageTask'.
                CancellationTokenSource cancellationTokenMBMessageReceived = new CancellationTokenSource(timeoutToGetSomeMBMessageTask);

                // The compositeTokenSource will be a composed token between the cancellationTokenMainTask and the cancellationTokenMBMessageReceived. The first ending returns the exception.
                CancellationTokenSource compositeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenMainTask.Token, cancellationTokenMBMessageReceived.Token);

                CancellationTokenSource cancellationTokenUninstallationQueued = new CancellationTokenSource(timeoutToGetSomeMBMessageTask);
                CancellationTokenSource compositeTokenSourceWithDeploymentQueued = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenMainTask.Token, cancellationTokenMBMessageReceived.Token, cancellationTokenUninstallationQueued.Token);

                await Task.Run(() => _progressTracker.ShowLoadingIndicator(compositeTokenSourceWithDeploymentQueued.Token));

                while (!this._isUninstallationFinished)
                {
                    if (_progressTracker.HasStarted && cancellationTokenUninstallationQueued != null && !cancellationTokenUninstallationQueued.IsCancellationRequested)
                    {
                        cancellationTokenUninstallationQueued.Cancel();
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
                            cancellationTokenUninstallationQueued?.Dispose();

                            throw new TaskCanceledException($"Uninstallation Failed! The uninstallation timed out after waiting {timeoutMainTask.TotalMinutes} minutes to finish.");
                        }

                        if (cancellationTokenMBMessageReceived.Token.IsCancellationRequested)
                        {
                            if (DeploymentProgressTrackerBase.UtcOfLastMessageReceived == null || (DateTime.UtcNow - DeploymentProgressTrackerBase.UtcOfLastMessageReceived.Value) >= timeoutToGetSomeMBMessageTask)
                            {
                                compositeTokenSource?.Dispose();
                                cancellationTokenMBMessageReceived?.Dispose();
                                cancellationTokenUninstallationQueued?.Dispose();
                                compositeTokenSourceWithDeploymentQueued?.Dispose();

                                throw new TaskCanceledException($"Uninstallation Failed! The uninstallation timed out after {timeoutToGetSomeMBMessageTask.TotalMinutes} minutes because the SDK client did not receive additional expected messages on MessageBus from the portal and the installation is not finished.");
                            }
                            else
                            {
                                cancellationTokenMBMessageReceived.Dispose();
                                compositeTokenSource.Dispose();
                                cancellationTokenMBMessageReceived = new CancellationTokenSource(timeoutToGetSomeMBMessageTask - (DateTime.UtcNow - DeploymentProgressTrackerBase.UtcOfLastMessageReceived.Value));
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
                cancellationTokenUninstallationQueued?.Dispose();
                compositeTokenSourceWithDeploymentQueued?.Dispose();
            }

            if (_hasUninstallationFailed)
            {
                throw new Exception("Uninstallation Failed! Check the logs for more information.");
            }
        }
    }
}
