using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.MessageBus.Client;
using Cmf.MessageBus.Messages;
using Moq;

namespace Common.UnitTests.Handlers;

public class AppUninstallationHandlerTests
{
    private readonly Mock<ISession> _sessionMock = new();
    private readonly Mock<ICustomerPortalClient> _customerPortalClientMock = new();
    private readonly Mock<IDeploymentProgressTrackerFactory> _progressTrackerFactoryMock = new();
    private readonly Mock<IDeploymentProgressTracker> _progressTrackerMock = new();
    private readonly Mock<IMessageBusTransport> _messageBusMock = new();

    private readonly AppUninstallationHandler _handler;

    public AppUninstallationHandlerTests()
    {
        _handler = new AppUninstallationHandler(
            _sessionMock.Object,
            _customerPortalClientMock.Object,
            _progressTrackerFactoryMock.Object
        );
    }

    private CustomerEnvironmentApplicationPackage ArrangeHandler(
        long id = 123,
        bool hasFinished = true,
        bool hasFailed = false
    )
    {
        var ceap = new CustomerEnvironmentApplicationPackage
        {
            Id = id
        };

        _customerPortalClientMock
            .Setup(c => c.GetMessageBusTransport())
            .ReturnsAsync(_messageBusMock.Object);

        _progressTrackerFactoryMock
            .Setup(f => f.CreateAppUninstallationTracker())
            .Returns(_progressTrackerMock.Object);

        _progressTrackerMock
            .SetupGet(t => t.HasFinished)
            .Returns(hasFinished);

        _progressTrackerMock
            .SetupGet(t => t.HasFailed)
            .Returns(hasFailed);

        _progressTrackerMock
            .Setup(t => t.ShowLoadingIndicator(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _customerPortalClientMock
            .Setup(
                c => c.StartAppUninstall(
                    It.IsAny<long>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()
                )
            )
            .Returns(Task.CompletedTask);

        return ceap;
    }


    [Fact]
    public async Task Handle_SubscribesProgressTrackerToMessageBus()
    {
        // Arrange
        const int id = 77;
        var ceap = ArrangeHandler(id);

        OnMbMessageCallback? subscribedHandler = null;

        _messageBusMock
            .Setup(
                m => m.Subscribe(
                    $"CUSTOMERPORTAL.DEPLOYMENT.APP.{id}", // also verifies that subject is as expected
                    It.IsAny<OnMbMessageCallback>()
                )
            )
            .Callback<string, OnMbMessageCallback>((_, handler) => { subscribedHandler = handler; });

        // Act
        await _handler.Handle(
            ceap,
            undeploy: false,
            timeoutMinutesMainTask: null,
            timeoutMinutesToGetSomeMBMsg: null
        );

        // Assert
        Assert.NotNull(subscribedHandler);

        subscribedHandler("test-subject", null!);

        _progressTrackerMock.Verify(
            t => t.ProcessDeploymentMessage("test-subject", It.IsAny<MbMessage>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WhenProgressTrackerReportsFailure_Throws()
    {
        // Arrange
        var ceap = ArrangeHandler(
            hasFinished: true,
            hasFailed: true
        );

        // Act
        var exception = await Assert.ThrowsAsync<Exception>(
            () =>
                _handler.Handle(
                    ceap,
                    undeploy: false,
                    timeoutMinutesMainTask: null,
                    timeoutMinutesToGetSomeMBMsg: null
                )
        );

        // Assert
        Assert.Equal(
            "Uninstallation Failed! Check the logs for more information.",
            exception.Message
        );

        _customerPortalClientMock.Verify(
            c => c.StartAppUninstall(ceap.Id, true, false, false),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WhenGetMessageBusTransportFails_DoesNotStartAppUninstall()
    {
        // Arrange
        var ceap = new CustomerEnvironmentApplicationPackage
        {
            Id = 123
        };

        var expectedException = new InvalidOperationException("MessageBus unavailable");

        _customerPortalClientMock
            .Setup(c => c.GetMessageBusTransport())
            .ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                _handler.Handle(
                    ceap,
                    undeploy: false,
                    timeoutMinutesMainTask: null,
                    timeoutMinutesToGetSomeMBMsg: null
                )
        );

        // Assert
        Assert.Same(expectedException, exception);

        _customerPortalClientMock.Verify(
            c => c.StartAppUninstall(
                It.IsAny<long>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_OrderOfOperations_IsCorrect()
    {
        // Arrange
        var ceap = new CustomerEnvironmentApplicationPackage { Id = 123 };

        var calls = new List<string>();

        _customerPortalClientMock
            .Setup(c => c.GetMessageBusTransport())
            .Callback(() => calls.Add("get-transport"))
            .ReturnsAsync(_messageBusMock.Object);

        _progressTrackerFactoryMock
            .Setup(f => f.CreateAppUninstallationTracker())
            .Callback(() => calls.Add("create-tracker"))
            .Returns(_progressTrackerMock.Object);

        _messageBusMock
            .Setup(
                m => m.Subscribe(
                    It.IsAny<string>(),
                    It.IsAny<OnMbMessageCallback>()
                )
            )
            .Callback(() => calls.Add("subscribe"));

        _customerPortalClientMock
            .Setup(c => c.StartAppUninstall(123, true, false, false))
            .Callback(() => calls.Add("start-uninstall"))
            .Returns(Task.CompletedTask);

        _progressTrackerMock
            .SetupGet(t => t.HasFinished)
            .Returns(true);

        _progressTrackerMock
            .SetupGet(t => t.HasFailed)
            .Returns(false);

        _progressTrackerMock
            .Setup(t => t.ShowLoadingIndicator(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(
            ceap,
            removeVolumes: false,
            undeploy: false,
            timeoutMinutesMainTask: null,
            timeoutMinutesToGetSomeMBMsg: null
        );

        // Assert
        Assert.Equal(["get-transport", "create-tracker", "subscribe", "start-uninstall"], calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_StartsAppUninstall_WithCorrectArguments(bool undeploy)
    {
        // Arrange
        var ceap = ArrangeHandler();

        // Act
        await _handler.Handle(
            ceap,
            removeVolumes: false,
            undeploy: undeploy,
            timeoutMinutesMainTask: null,
            timeoutMinutesToGetSomeMBMsg: null
        );

        // Assert
        _customerPortalClientMock.Verify(
            c => c.StartAppUninstall(
                123,
                removeDeployments: true,
                removeVolumes: undeploy,
                undeploy: undeploy
            ),
            Times.Once
        );
    }
    
    [Theory]
    [InlineData(true,     false,         true)]   // forced by undeploy
    [InlineData(false,    false,         false)]  // pass-through (false)
    [InlineData(false,    true,          true)]   // pass-through (true)
    public async Task Handle_RemoveVolumes_IsForcedByUndeploy_OtherwisePassthrough(
        bool undeploy,
        bool removeVolumes,
        bool expectedRemoveVolumes)
    {
        // Arrange
        var ceap = ArrangeHandler();

        // Act
        await _handler.Handle(
            ceap,
            removeVolumes: removeVolumes,
            undeploy: undeploy,
            timeoutMinutesMainTask: null,
            timeoutMinutesToGetSomeMBMsg: null
        );

        // Assert
        _customerPortalClientMock.Verify(
            c => c.StartAppUninstall(
                123,
                removeDeployments: true,
                removeVolumes: expectedRemoveVolumes,
                undeploy: undeploy
            ),
            Times.Once
        );
    }
}
