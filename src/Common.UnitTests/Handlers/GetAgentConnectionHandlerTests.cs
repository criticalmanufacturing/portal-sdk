using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Moq;

namespace Common.UnitTests.Handlers;

public class GetAgentConnectionHandlerTests
{
    private readonly Mock<ICustomerPortalClient> _customerPortalClientMock = new();
    private readonly Mock<ISession> _sessionMock = new();
    private readonly Mock<ICustomerEnvironmentServices> _customerEnvironmentServicesMock = new();

    private readonly GetAgentConnectionHandler _handler;

    public GetAgentConnectionHandlerTests()
    {
        _handler = new GetAgentConnectionHandler(
            _customerPortalClientMock.Object,
            _sessionMock.Object);
    }

    [Fact]
    public async Task Run_WhenAgentNameIsProvided_ChecksConnectionByAgent()
    {
        var agentName = "agent-a";
        var definitionId = 1001010000060000044;
        var agent = new CustomerEnvironment { DefinitionId = definitionId };

        _customerPortalClientMock
            .Setup(x => x.GetObjectByName<CustomerEnvironment>(agentName, 0))
            .ReturnsAsync(agent);
        _customerPortalClientMock
            .Setup(x => x.CheckCustomerEnvironmentConnectionStatus(definitionId))
            .ReturnsAsync(true);

        var result = await _handler.Run(agentName, string.Empty);

        Assert.True(result);
        _sessionMock.Verify(x => x.RestoreSession(), Times.Once);
        _customerPortalClientMock.Verify(x => x.GetObjectByName<CustomerEnvironment>(agentName, 0), Times.Once);
        _customerPortalClientMock.Verify(x => x.GetCustomerInfrastructureAgentByCustomerEnvironment(It.IsAny<string>()), Times.Never);
        _customerPortalClientMock.Verify(x => x.CheckCustomerEnvironmentConnectionStatus(definitionId), Times.Once);
    }

    [Fact]
    public async Task Run_WhenCustomerEnvironmentNameIsProvided_ChecksConnectionByInfrastructureAgent()
    {
        var customerEnvironmentName = "dev-env";
        var definitionId = 1001010000060000044;
        var infrastructureAgent = new CustomerEnvironment { DefinitionId = definitionId };

        _customerPortalClientMock
            .Setup(x => x.GetCustomerInfrastructureAgentByCustomerEnvironment(customerEnvironmentName))
            .ReturnsAsync(infrastructureAgent);
        _customerPortalClientMock
            .Setup(x => x.CheckCustomerEnvironmentConnectionStatus(definitionId))
            .ReturnsAsync(false);

        var result = await _handler.Run(string.Empty, customerEnvironmentName);

        Assert.False(result);
        _sessionMock.Verify(x => x.RestoreSession(), Times.Once);
        _customerPortalClientMock.Verify(x => x.GetObjectByName<CustomerEnvironment>(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _customerPortalClientMock.Verify(x => x.GetCustomerInfrastructureAgentByCustomerEnvironment(customerEnvironmentName), Times.Once);
        _customerPortalClientMock.Verify(x => x.CheckCustomerEnvironmentConnectionStatus(definitionId), Times.Once);
    }

    [Fact]
    public async Task Run_WhenCustomerEnvironmentExistsHasNoAgentHasNoInfra_ReturnsFalse()
    {
        var customerEnvironmentName = "env-no-agent-no-infra";

        _customerPortalClientMock
            .Setup(x => x.GetCustomerInfrastructureAgentByCustomerEnvironment(customerEnvironmentName))
            .ReturnsAsync((CustomerEnvironment)null!);

        var result = await _handler.Run(string.Empty, customerEnvironmentName);

        // Verify Returns False (Environment exists, has no agent, has no infra)
        Assert.False(result);
        _sessionMock.Verify(x => x.RestoreSession(), Times.Once);
        _customerPortalClientMock.Verify(x => x.GetObjectByName<CustomerEnvironment>(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _customerPortalClientMock.Verify(x => x.GetCustomerInfrastructureAgentByCustomerEnvironment(customerEnvironmentName), Times.Once);
        _customerPortalClientMock.Verify(x => x.CheckCustomerEnvironmentConnectionStatus(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Run_WhenCustomerEnvironmentExistsHasDisconnectedAgentHasInfra_ReturnsFalse()
    {
        var customerEnvironmentName = "env-disconnected-agent";
        var definitionId = 1001010000060000044;
        var infrastructureAgent = new CustomerEnvironment { DefinitionId = definitionId };

        _customerPortalClientMock
            .Setup(x => x.GetCustomerInfrastructureAgentByCustomerEnvironment(customerEnvironmentName))
            .ReturnsAsync(infrastructureAgent);
        _customerPortalClientMock
            .Setup(x => x.CheckCustomerEnvironmentConnectionStatus(definitionId))
            .ReturnsAsync(false);

        var result = await _handler.Run(string.Empty, customerEnvironmentName);

        // Verify Returns False (Environment exists, has agent (disconnected), has infra)
        Assert.False(result);
        _sessionMock.Verify(x => x.RestoreSession(), Times.Once);
        _customerPortalClientMock.Verify(x => x.GetObjectByName<CustomerEnvironment>(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _customerPortalClientMock.Verify(x => x.GetCustomerInfrastructureAgentByCustomerEnvironment(customerEnvironmentName), Times.Once);
        _customerPortalClientMock.Verify(x => x.CheckCustomerEnvironmentConnectionStatus(definitionId), Times.Once);
    }

    [Fact]
    public async Task Run_WhenCustomerEnvironmentDoesNotExist_ThrowsException()
    {
        var customerEnvironmentName = "env-does-not-exist";

        _customerPortalClientMock
            .Setup(x => x.GetCustomerInfrastructureAgentByCustomerEnvironment(customerEnvironmentName))
            .ThrowsAsync(new NotFoundException($"No customer environment found for name {customerEnvironmentName}"));

        // Verify Throw Exception (environment doesn't exist, has no agent, has no infra)
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Run(string.Empty, customerEnvironmentName));

        _sessionMock.Verify(x => x.RestoreSession(), Times.Once);
        _customerPortalClientMock.Verify(x => x.GetObjectByName<CustomerEnvironment>(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _customerPortalClientMock.Verify(x => x.GetCustomerInfrastructureAgentByCustomerEnvironment(customerEnvironmentName), Times.Once);
        _customerPortalClientMock.Verify(x => x.CheckCustomerEnvironmentConnectionStatus(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Run_WhenCustomerEnvironmentExistsHasConnectedAgent_ReturnsTrue()
    {
        var customerEnvironmentName = "env-connected-agent";
        var definitionId = 1001010000060000044;
        var infrastructureAgent = new CustomerEnvironment { DefinitionId = definitionId };

        _customerPortalClientMock
            .Setup(x => x.GetCustomerInfrastructureAgentByCustomerEnvironment(customerEnvironmentName))
            .ReturnsAsync(infrastructureAgent);
        _customerPortalClientMock
            .Setup(x => x.CheckCustomerEnvironmentConnectionStatus(definitionId))
            .ReturnsAsync(true);

        var result = await _handler.Run(string.Empty, customerEnvironmentName);

        // Verify Returns True (environment exists and has agent)
        Assert.True(result);
        _sessionMock.Verify(x => x.RestoreSession(), Times.Once);
        _customerPortalClientMock.Verify(x => x.GetObjectByName<CustomerEnvironment>(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _customerPortalClientMock.Verify(x => x.GetCustomerInfrastructureAgentByCustomerEnvironment(customerEnvironmentName), Times.Once);
        _customerPortalClientMock.Verify(x => x.CheckCustomerEnvironmentConnectionStatus(definitionId), Times.Once);
    }

    [Fact]
    public async Task Run_WhenCustomerEnvironmentExistsHasNoAgentHasInfra_ReturnsFalse()
    {
        var customerEnvironmentName = "env-no-agent-has-infra";

        _customerPortalClientMock
            .Setup(x => x.GetCustomerInfrastructureAgentByCustomerEnvironment(customerEnvironmentName))
            .ReturnsAsync((CustomerEnvironment)null!);

        var result = await _handler.Run(string.Empty, customerEnvironmentName);

        // Verify Returns False (environments exist, has no agent, has infra)
        Assert.False(result);
        _sessionMock.Verify(x => x.RestoreSession(), Times.Once);
        _customerPortalClientMock.Verify(x => x.GetObjectByName<CustomerEnvironment>(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _customerPortalClientMock.Verify(x => x.GetCustomerInfrastructureAgentByCustomerEnvironment(customerEnvironmentName), Times.Once);
        _customerPortalClientMock.Verify(x => x.CheckCustomerEnvironmentConnectionStatus(It.IsAny<long>()), Times.Never);
    }
}