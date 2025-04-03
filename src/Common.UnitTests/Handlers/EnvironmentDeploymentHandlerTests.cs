using Cmf.CustomerPortal.Deployment.Models;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.MessageBus.Messages;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.UnitTests.Handlers
{
    public class EnvironmentDeploymentHandlerTests
    {
        [Fact]
        public void ProcessDeploymentMessageQueuePosition_ShouldNotCallDeserializeWhenHasInstallationStarted_Is_True()
        {
            // Arrange

            var mockSession = new Mock<ISession>();
            var mockCustomerPortalClient = new Mock<ICustomerPortalClient>();
            var mockArtifactsDownloadHandler = new Mock<IArtifactsDownloaderHandler>();
            var mockJsonHelper = new Mock<IJsonHelper>();
            EnvironmentDeploymentHandler environmentDeploymentHandler = new EnvironmentDeploymentHandler(mockSession.Object, mockCustomerPortalClient.Object, mockArtifactsDownloadHandler.Object, mockJsonHelper.Object);

            // Act
            var emptyJson = "{ \"Data\": \"\" }";
            MbMessage mbMessage = new MbMessage()
            {
                Data = emptyJson,
            };

            environmentDeploymentHandler._hasDeploymentStarted = true;
            environmentDeploymentHandler.ProcessDeploymentMessageQueuePosition("subject", mbMessage);

            //Assert
            mockJsonHelper.Verify(x => x.Deserialize<DeploymentProgressMessage>(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void ProcessDeploymentMessageQueuePosition_Should_Call_Deserialize_When_Valid_Json_Provided()
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