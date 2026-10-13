using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Cmf.CustomerPortal.Sdk.Common;
using Moq;

namespace Common.UnitTests;

public class MessageBusTokenRefresherTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ISession> _sessionMock = new();
    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly List<MessageBusTokens> _appliedTokens = [];
    private Func<Task<MessageBusTokens>> _fetchTokens = () => Task.FromResult(new MessageBusTokens(CreateToken(Now.AddHours(1)), "data-groups"));
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

    private static string CreateToken(DateTimeOffset? expires) =>
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims: [new Claim("tenantName", "CustomerPortal")],
            expires: expires?.UtcDateTime));

    [Fact]
    public void GetRefreshDelay_LongLivedToken_RefreshesFiveMinutesBeforeExpiry()
    {
        // Arrange
        string token = CreateToken(Now.AddHours(1));

        // Act
        TimeSpan? delay = MessageBusTokenRefresher.GetRefreshDelay(token, Now);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(55), delay);
    }

    [Fact]
    public void GetRefreshDelay_ShortLivedToken_RefreshesAtFourFifthsOfRemainingLifetime()
    {
        // Arrange
        string token = CreateToken(Now.AddMinutes(10));

        // Act
        TimeSpan? delay = MessageBusTokenRefresher.GetRefreshDelay(token, Now);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(8), delay);
    }

    [Fact]
    public void GetRefreshDelay_ExpiredToken_ReturnsMinimumDelay()
    {
        // Arrange
        string token = CreateToken(Now.AddMinutes(-10));

        // Act
        TimeSpan? delay = MessageBusTokenRefresher.GetRefreshDelay(token, Now);

        // Assert
        Assert.Equal(MessageBusTokenRefresher.MinRefreshDelay, delay);
    }

    [Fact]
    public void GetRefreshDelay_TokenWithoutExpiration_ReturnsNull()
    {
        // Arrange
        string token = CreateToken(null);

        // Act
        TimeSpan? delay = MessageBusTokenRefresher.GetRefreshDelay(token, Now);

        // Assert
        Assert.Null(delay);
    }

    [Fact]
    public void GetRefreshDelay_NotAJwt_ReturnsNull()
    {
        // Act
        TimeSpan? delay = MessageBusTokenRefresher.GetRefreshDelay("not-a-jwt", Now);

        // Assert
        Assert.Null(delay);
    }

    [Fact]
    public void Start_SchedulesRefreshBeforeTokenExpires()
    {
        // Arrange
        var refresher = CreateRefresher();

        // Act
        refresher.Start(CreateToken(Now.AddHours(1)));

        // Assert
        FakeTimer timer = Assert.Single(_timeProvider.Timers);
        Assert.Equal(TimeSpan.FromMinutes(55), timer.DueTime);
        Assert.Equal(0, _fetchCount);
    }

    [Fact]
    public void Start_TokenWithoutExpiration_DoesNotScheduleRefresh()
    {
        // Arrange
        var refresher = CreateRefresher();

        // Act
        refresher.Start(CreateToken(null));

        // Assert
        Assert.Empty(_timeProvider.Timers);
    }

    [Fact]
    public void ScheduledRefresh_AppliesNewTokensAndSchedulesNextRefresh()
    {
        // Arrange
        var refresher = CreateRefresher();
        refresher.Start(CreateToken(Now.AddHours(1)));
        _timeProvider.Now = Now.AddMinutes(55);
        string newToken = CreateToken(Now.AddHours(2));
        _fetchTokens = () => Task.FromResult(new MessageBusTokens(newToken, "new-data-groups"));

        // Act
        _timeProvider.Timers[0].Fire();

        // Assert
        MessageBusTokens applied = Assert.Single(_appliedTokens);
        Assert.Equal(newToken, applied.SecurityToken);
        Assert.Equal("new-data-groups", applied.DataGroupsToken);
        Assert.True(_timeProvider.Timers[0].IsDisposed);
        Assert.Equal(TimeSpan.FromMinutes(60), _timeProvider.Timers[1].DueTime);
    }

    [Fact]
    public async Task RefreshAsync_FetchFails_LogsAndRetriesLater()
    {
        // Arrange
        var refresher = CreateRefresher();
        _fetchTokens = () => Task.FromException<MessageBusTokens>(new HttpRequestException("portal unavailable"));

        // Act
        await refresher.RefreshAsync();

        // Assert
        Assert.Empty(_appliedTokens);
        FakeTimer retry = Assert.Single(_timeProvider.Timers);
        Assert.Equal(MessageBusTokenRefresher.MinRefreshDelay, retry.DueTime);
        _sessionMock.Verify(s => s.LogDebug(It.Is<string>(m => m.Contains("portal unavailable"))), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_RefreshAlreadyScheduled_DoesNothing()
    {
        // Arrange
        var refresher = CreateRefresher();
        refresher.Start(CreateToken(Now.AddHours(1)));

        // Act
        await refresher.RefreshAsync();

        // Assert
        Assert.Equal(0, _fetchCount);
        Assert.Empty(_appliedTokens);
        FakeTimer timer = Assert.Single(_timeProvider.Timers);
        Assert.False(timer.IsDisposed);
    }

    [Fact]
    public async Task RefreshAsync_RetryScheduledAfterFailure_DoesNothing()
    {
        // Arrange
        var refresher = CreateRefresher();
        _fetchTokens = () => Task.FromException<MessageBusTokens>(new HttpRequestException("portal unavailable"));
        await refresher.RefreshAsync();

        // Act
        await refresher.RefreshAsync();

        // Assert
        Assert.Equal(1, _fetchCount);
        Assert.Single(_timeProvider.Timers);
    }

    [Fact]
    public void OnTransportException_UnauthorizedWhileRefreshScheduled_DoesNotRefreshTokens()
    {
        // Arrange
        var refresher = CreateRefresher();
        refresher.Start(CreateToken(Now.AddHours(1)));

        // Act
        refresher.OnTransportException("HTTP/1.1 401 Unauthorized");

        // Assert
        Assert.Equal(0, _fetchCount);
        Assert.Empty(_appliedTokens);
    }

    [Fact]
    public void OnTransportException_Unauthorized_RefreshesTokens()
    {
        // Arrange
        var refresher = CreateRefresher();

        // Act
        refresher.OnTransportException("HTTP/1.1 401 Unauthorized");

        // Assert
        Assert.Single(_appliedTokens);
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
    public void OnTransportException_RepeatedUnauthorized_RefreshesOncePerMinimumDelay()
    {
        // Arrange: tokens without expiration, so no refresh gets scheduled between the 401s
        var refresher = CreateRefresher();
        _fetchTokens = () => Task.FromResult(new MessageBusTokens(CreateToken(null), "data-groups"));

        // Act
        refresher.OnTransportException("HTTP/1.1 401 Unauthorized");
        _timeProvider.Now = Now.AddSeconds(10);
        refresher.OnTransportException("HTTP/1.1 401 Unauthorized");
        _timeProvider.Now = Now + MessageBusTokenRefresher.MinRefreshDelay;
        refresher.OnTransportException("HTTP/1.1 401 Unauthorized");

        // Assert
        Assert.Equal(2, _fetchCount);
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

        public List<FakeTimer> Timers { get; } = [];

        public override DateTimeOffset GetUtcNow() => Now;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new FakeTimer(() => callback(state), dueTime);
            Timers.Add(timer);
            return timer;
        }
    }

    private sealed class FakeTimer(Action callback, TimeSpan dueTime) : ITimer
    {
        public TimeSpan DueTime { get; } = dueTime;

        public bool IsDisposed { get; private set; }

        public void Fire() => callback();

        public bool Change(TimeSpan dueTime, TimeSpan period) => throw new NotSupportedException();

        public void Dispose() => IsDisposed = true;

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
