using Moq;
using Newtonsoft.Json;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.MessageBus.Messages;

namespace Common.UnitTests.Services
{
    public class DeploymentProgressTrackerTests
    {
        [Fact]
        public void EnvironmentAdapter_IsStartedAndIsFinished_Behavior()
        {
            var adapter = new EnvironmentDeploymentStatusAdapter();

            // Deploying -> started
            Assert.True(adapter.IsStarted(DeploymentStatus.Deploying));

            // Terminating -> started
            Assert.True(adapter.IsStarted(DeploymentStatus.Terminating));

            // DeploymentSucceeded -> finished, not failed
            Assert.True(adapter.IsFinished(DeploymentStatus.DeploymentSucceeded, out bool failedSucc));
            Assert.False(failedSucc);

            // DeploymentPartiallySucceeded -> finished, not failed
            Assert.True(adapter.IsFinished(DeploymentStatus.DeploymentPartiallySucceeded, out bool failedPart));
            Assert.False(failedPart);

            // DeploymentFailed -> finished and failed
            Assert.True(adapter.IsFinished(DeploymentStatus.DeploymentFailed, out bool failedFail));
            Assert.True(failedFail);

            // NotDeployed -> not started, not finished
            Assert.False(adapter.IsStarted(DeploymentStatus.NotDeployed));
            Assert.False(adapter.IsFinished(DeploymentStatus.NotDeployed, out _));
        }

        [Fact]
        public void AppInstallationAdapter_IsStartedAndIsFinished_Behavior()
        {
            var adapter = new AppInstallationStatusAdapter();

            // Installing -> started
            Assert.True(adapter.IsStarted(AppInstallationStatus.Installing));

            // Uninstalling -> started
            Assert.True(adapter.IsStarted(AppInstallationStatus.Uninstalling));

            // InstallationSucceeded -> finished, not failed
            Assert.True(adapter.IsFinished(AppInstallationStatus.InstallationSucceeded, out bool failedSucc));
            Assert.False(failedSucc);

            // InstallationFailed -> finished and failed
            Assert.True(adapter.IsFinished(AppInstallationStatus.InstallationFailed, out bool failedFail));
            Assert.True(failedFail);

            // NotInstalled -> not started, not finished
            Assert.False(adapter.IsStarted(AppInstallationStatus.NotInstalled));
            Assert.False(adapter.IsFinished(AppInstallationStatus.NotInstalled, out _));
        }

        [Fact]
        public void AppUninstallationAdapter_IsStartedAndIsFinished_Behavior()
        {
            var adapter = new AppUninstallationStatusAdapter();

            // Uninstalling -> started
            Assert.True(adapter.IsStarted(AppInstallationStatus.Uninstalling));

            // UninstallationSucceeded -> finished, not failed
            Assert.True(adapter.IsFinished(AppInstallationStatus.UninstallationSucceeded, out bool failedSucc));
            Assert.False(failedSucc);

            // UninstallationFailed -> finished and failed
            Assert.True(adapter.IsFinished(AppInstallationStatus.UninstallationFailed, out bool failedFail));
            Assert.True(failedFail);

            // Installing -> not started for uninstallation adapter
            Assert.False(adapter.IsStarted(AppInstallationStatus.Installing));
            Assert.False(adapter.IsFinished(AppInstallationStatus.InstallationSucceeded, out _));
        }

        [Fact]
        public void DeploymentProgressTracker_ProcessDeploymentMessage_QueuePositionAndStateTransitions()
        {
            var sessionMock = new Mock<ISession>();
            var tracker = new DeploymentProgressTracker(sessionMock.Object, new EnvironmentDeploymentStatusAdapter());

            var payloadQueued = new
            {
                Data = "Queue Position: 2\n",
                DeploymentStatus = DeploymentStatus.NotDeployed
            };
            var messageQueued = new MbMessage { Data = JsonConvert.SerializeObject(payloadQueued) };

            tracker.ProcessDeploymentMessage("subject", messageQueued);

            sessionMock.Verify(s => s.LogInformation("Queue Position: 2"), Times.Once);
            Assert.False(tracker.HasStarted);
            Assert.False(tracker.HasFinished);
            Assert.NotNull(DeploymentProgressTracker.UtcOfLastMessageReceived);

            var payloadStarted = new
            {
                Data = "Starting deployment...",
                DeploymentStatus = DeploymentStatus.Deploying
            };
            var messageStarted = new MbMessage { Data = JsonConvert.SerializeObject(payloadStarted) };

            tracker.ProcessDeploymentMessage("subject", messageStarted);

            sessionMock.Verify(s => s.LogInformation("Starting deployment..."), Times.Once);
            Assert.True(tracker.HasStarted);
            Assert.False(tracker.HasFinished);

            var payloadFinished = new
            {
                Data = "Deployment finished.",
                DeploymentStatus = DeploymentStatus.DeploymentSucceeded
            };
            var messageFinished = new MbMessage { Data = JsonConvert.SerializeObject(payloadFinished) };

            tracker.ProcessDeploymentMessage("subject", messageFinished);

            sessionMock.Verify(s => s.LogInformation("Deployment finished."), Times.Once);
            Assert.True(tracker.HasFinished);
            Assert.False(tracker.HasFailed);
        }

        [Fact]
        public void DeploymentProgressTracker_ProcessDeploymentMessage_UnknownOrNullMessage_LogsUnknown()
        {
            var sessionMock = new Mock<ISession>();
            var tracker = new DeploymentProgressTracker(sessionMock.Object, new EnvironmentDeploymentStatusAdapter());

            var messageNull = new MbMessage { Data = null };
            tracker.ProcessDeploymentMessage("subject", messageNull);
            sessionMock.Verify(s => s.LogInformation("Unknown message received"), Times.Once);

            var messageEmpty = new MbMessage { Data = string.Empty };
            tracker.ProcessDeploymentMessage("subject", messageEmpty);
            sessionMock.Verify(s => s.LogInformation("Unknown message received"), Times.Exactly(2));
        }
    }
}