using Cmf.CustomerPortal.Sdk.Common;
using Moq;

namespace Common.UnitTests;

public class MessageBusTokenRefresherTests
{
    private const string Unauthorized = "HTTP/1.1 401 Unauthorized";
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ISession> _sessionMock = new();
    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly List<MessageBusTokens> _appliedTokens = [];
    private Func<Task<MessageBusTokens>> _fetchTokens = () => Task.FromResult(new MessageBusTokens("security-token", "data-groups-token"));
    private int _fetchCount;

    private MessageBusTokenRefresher CreateRefresher() => new(
        _sessionMock.Object,
        () =>
        {
            _fetchCount++;
            return _fetchTokens();
        },
        _appliedTokens.Add,
        _timeProvider);

    [Fact]
    public void OnTransportException_Unauthorized_AppliesNewTokens()
    {
        // Arrange
        var refresher = CreateRefresher();

        // Act
        refresher.OnTransportException(Unauthorized);

        // Assert
        MessageBusTokens applied = Assert.Single(_appliedTokens);
        Assert.Equal("security-token", applied.SecurityToken);
        Assert.Equal("data-groups-token", applied.DataGroupsToken);
        _sessionMock.Verify(s => s.LogDebug("MessageBus security token refreshed."), Times.Once);
    }

    [Fact]
    public void OnTransportException_OtherError_DoesNotRefreshTokens()
    {
        // Arrange
        var refresher = CreateRefresher();

        // Act
        refresher.OnTransportException("Connection refused");

        // Assert
        Assert.Equal(0, _fetchCount);
        Assert.Empty(_appliedTokens);
    }

    [Fact]
    public void OnTransportException_RepeatedUnauthorized_RefreshesOncePerMinimumInterval()
    {
        // Arrange
        var refresher = CreateRefresher();

        // Act
        refresher.OnTransportException(Unauthorized);
        _timeProvider.Now = Now.AddSeconds(10);
        refresher.OnTransportException(Unauthorized);
        _timeProvider.Now = Now + MessageBusTokenRefresher.MinRefreshInterval;
        refresher.OnTransportException(Unauthorized);

        // Assert
        Assert.Equal(2, _fetchCount);
        Assert.Equal(2, _appliedTokens.Count);
    }

    [Fact]
    public void OnTransportException_FetchFails_LogsAndDoesNotApplyTokens()
    {
        // Arrange
        var refresher = CreateRefresher();
        _fetchTokens = () => Task.FromException<MessageBusTokens>(new HttpRequestException("portal unavailable"));

        // Act
        refresher.OnTransportException(Unauthorized);

        // Assert
        Assert.Empty(_appliedTokens);
        _sessionMock.Verify(s => s.LogDebug(It.Is<string>(m => m.Contains("portal unavailable"))), Times.Once);
    }

    [Fact]
    public void OnTransportException_UnauthorizedAfterFailedRefresh_RetriesAfterMinimumInterval()
    {
        // Arrange
        var refresher = CreateRefresher();
        Func<Task<MessageBusTokens>> succeed = _fetchTokens;
        _fetchTokens = () => Task.FromException<MessageBusTokens>(new HttpRequestException("portal unavailable"));
        refresher.OnTransportException(Unauthorized);
        _fetchTokens = succeed;

        // Act
        _timeProvider.Now = Now.AddSeconds(10);
        refresher.OnTransportException(Unauthorized);
        _timeProvider.Now = Now + MessageBusTokenRefresher.MinRefreshInterval;
        refresher.OnTransportException(Unauthorized);

        // Assert
        Assert.Equal(2, _fetchCount);
        Assert.Single(_appliedTokens);
    }

    [Fact]
    public void OnTransportException_RefreshInProgress_DoesNotStartAnother()
    {
        // Arrange
        var refresher = CreateRefresher();
        var pendingFetch = new TaskCompletionSource<MessageBusTokens>();
        _fetchTokens = () => pendingFetch.Task;
        refresher.OnTransportException(Unauthorized);

        // Act
        _timeProvider.Now = Now + MessageBusTokenRefresher.MinRefreshInterval;
        refresher.OnTransportException(Unauthorized);

        // Assert
        Assert.Equal(1, _fetchCount);
        Assert.Empty(_appliedTokens);
    }

    [Fact]
    public void Constructor_NullArguments_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MessageBusTokenRefresher(null!, _fetchTokens, _appliedTokens.Add));
        Assert.Throws<ArgumentNullException>(() => new MessageBusTokenRefresher(_sessionMock.Object, null!, _appliedTokens.Add));
        Assert.Throws<ArgumentNullException>(() => new MessageBusTokenRefresher(_sessionMock.Object, _fetchTokens, null!));
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
