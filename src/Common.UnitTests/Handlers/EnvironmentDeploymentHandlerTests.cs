using Cmf.CustomerPortal.Deployment.Models;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.MessageBus.Messages;
using Moq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Common.UnitTests.Handlers
{
    public class EnvironmentDeploymentHandlerTests
    {
        [Fact]
        public void ProcessDeploymentMessageQueuePosition_ShouldNotLogUnknownMessageReceived_WhenRegexMatches()
        {
            // Arrange
            var mockSession = new Mock<ISession>();
            var mockCustomerPortalClient = new Mock<ICustomerPortalClient>();
            var mockArtifactsDownloadHandler = new Mock<IArtifactsDownloaderHandler>();

            EnvironmentDeploymentHandler appInstallationHandler = new EnvironmentDeploymentHandler(mockSession.Object, mockCustomerPortalClient.Object, mockArtifactsDownloadHandler.Object);

            var validJson = "{\"DeploymentOperation\":0,\"Data\":\"Queue Position: 1\n\"}";
            var message = new MbMessage { Data = validJson };

            // Act
            appInstallationHandler.ProcessDeploymentMessage("someSubject", message);

            // Assert
            mockSession.Verify(x => x.LogInformation("Unknown message received"), Times.Never);
        }

        [Fact]
        public void ProcessDeploymentMessageQueuePosition_ShouldCallLogInformation_WhenRegexDoesNotMatch()
        {
            // Arrange
            var mockSession = new Mock<ISession>();

            var mockCustomerPortalClient = new Mock<ICustomerPortalClient>();
            var mockArtifactsDownloadHandler = new Mock<IArtifactsDownloaderHandler>();

            EnvironmentDeploymentHandler appInstallationHandler = new EnvironmentDeploymentHandler(mockSession.Object, mockCustomerPortalClient.Object, mockArtifactsDownloadHandler.Object);
            var message = new MbMessage { Data = null };

            // Act
            appInstallationHandler.ProcessDeploymentMessage("someSubject", message);

            // Assert
            mockSession.Verify(x => x.LogInformation("Unknown message received"), Times.Once);
        }

    }
}