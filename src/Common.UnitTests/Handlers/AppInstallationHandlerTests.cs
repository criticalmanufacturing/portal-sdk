using Autofac.Extras.Moq;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.MessageBus.Messages;
using Moq;

namespace Common.UnitTests.Handlers
{
    public class AppInstallationHandlerTests
    {
 
        [Fact]
        public void ProcessDeploymentMessageQueuePosition_ShouldNotLogUnknownMessageReceived_WhenRegexMatches()
        {
            // Arrange
            var mock = AutoMock.GetLoose();
            var mockSession = mock.Mock<ISession>();
            
            var appInstallationHandler = mock.Create<AppInstallationHandler>();
            var message = new MbMessage { Data = "{\"DeploymentOperation\":0,\"Data\":\"Queue Position: 1\n\"}" };

            // Act
            appInstallationHandler.ProcessDeploymentMessage("someSubject", message);

            // Assert
            mockSession.Verify(x => x.LogInformation("Unknown message received"), Times.Never);
        }

        [Fact]
        public void ProcessDeploymentMessageQueuePosition_ShouldCallLogInformation_WhenRegexDoesNotMatch()
        {
            // Arrange
            var mock = AutoMock.GetLoose();
            var mockSession = mock.Mock<ISession>();

            var appInstallationHandler = mock.Create<AppInstallationHandler>();
            var message = new MbMessage { Data = null };

            // Act
            appInstallationHandler.ProcessDeploymentMessage("someSubject", message);

            // Assert
            mockSession.Verify(x => x.LogInformation("Unknown message received"), Times.Once);
        }

    }
}
