using Cmf.CustomerPortal.Deployment.Models;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.MessageBus.Messages;
using Moq;

namespace Common.UnitTests.Handlers
{
    public class AppInstallationHandlerTests
    {
        [Fact]
        public void ProcessDeploymentMessageQueuePosition_ShouldNotCallDeserializeWhenHasInstallationStarted_IsTrue()
        {
            // Arrange

            var mockSession = new Mock<ISession>();
            var mockCustomerPortalClient = new Mock<ICustomerPortalClient>();
            var mockArtifactsDownloadHandler = new Mock<IArtifactsDownloaderHandler>();
            var mockJsonHelper = new Mock<IJsonHelper>();
            AppInstallationHandler appInstallationHandler = new AppInstallationHandler(mockSession.Object, mockCustomerPortalClient.Object, mockArtifactsDownloadHandler.Object, mockJsonHelper.Object);

            // Act
            var emptyJson = "{ \"Data\": \"\" }";
            MbMessage mbMessage = new MbMessage()
            {
                Data = emptyJson,
            };
            
            appInstallationHandler.HasInstallationStarted = true;
            appInstallationHandler.ProcessDeploymentMessageQueuePosition("subject", mbMessage);

            //Assert
            mockJsonHelper.Verify(x => x.Deserialize<DeploymentProgressMessage>(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void ProcessDeploymentMessageQueuePosition_ShouldCallDeserialize_WhenValidJsonProvided()
        {
            // Arrange
            var mockSession = new Mock<ISession>();
            var mockJsonHelper = new Mock<IJsonHelper>();
            var mockCustomerPortalClient = new Mock<ICustomerPortalClient>();
            var mockArtifactsDownloadHandler = new Mock<IArtifactsDownloaderHandler>();

            AppInstallationHandler appInstallationHandler = new AppInstallationHandler(mockSession.Object, mockCustomerPortalClient.Object, mockArtifactsDownloadHandler.Object, mockJsonHelper.Object);

            var validJson = "{ \"Data\": \"Queue position: 5\" }";
            var message = new MbMessage { Data = validJson };

            // Simulate correct deserialization
            mockJsonHelper.Setup(x => x.Deserialize<DeploymentProgressMessage>(It.IsAny<string>()))
                .Returns(new DeploymentProgressMessage { Data = "Queue position: 5" });

            // Act
            appInstallationHandler.ProcessDeploymentMessageQueuePosition("someSubject", message);

            // Assert
            mockJsonHelper.Verify(x => x.Deserialize<DeploymentProgressMessage>(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void ProcessDeploymentMessageQueuePosition_ShouldCallLogInformation_WhenRegexDoesNotMatch()
        {
            // Arrange
            var mockSession = new Mock<ISession>();
            var mockJsonHelper = new Mock<IJsonHelper>();

            var mockCustomerPortalClient = new Mock<ICustomerPortalClient>();
            var mockArtifactsDownloadHandler = new Mock<IArtifactsDownloaderHandler>();

            AppInstallationHandler appInstallationHandler = new AppInstallationHandler(mockSession.Object, mockCustomerPortalClient.Object, mockArtifactsDownloadHandler.Object, mockJsonHelper.Object);

            var invalidJson = "{ \"Data\": \"No match here\" }";
            var message = new MbMessage { Data = invalidJson };

            // Simulate correct deserialization but invalid data
            mockJsonHelper.Setup(x => x.Deserialize<DeploymentProgressMessage>(It.IsAny<string>()))
                .Returns(new DeploymentProgressMessage { Data = "No match here" });

            // Act
            appInstallationHandler.ProcessDeploymentMessageQueuePosition("someSubject", message);

            // Assert
            mockSession.Verify(x => x.LogInformation(It.IsAny<string>()), Times.Once);
        }

    }
}
