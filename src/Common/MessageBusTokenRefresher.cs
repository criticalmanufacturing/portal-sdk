using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common;

/// <summary>
/// MessageBus credentials issued by the portal's application boot information.
/// </summary>
/// <param name="SecurityToken">JWT the MessageBus gateway validates on every (re)connection handshake.</param>
/// <param name="DataGroupsToken">Token that grants access to the data groups.</param>
internal sealed record MessageBusTokens(string SecurityToken, string DataGroupsToken);

/// <summary>
/// Keeps the MessageBus transport's tokens valid for long-running operations.
/// The transport reuses its security token on every automatic reconnect, so once the token expires
/// each reconnect is rejected with <c>HTTP/1.1 401 Unauthorized</c>. This class fetches new tokens
/// shortly before the current one expires, and again whenever the transport reports a 401.
/// </summary>
/// <param name="session">Session used for logging.</param>
/// <param name="fetchTokens">Fetches new tokens from the portal.</param>
/// <param name="applyTokens">Applies the new tokens to the transport.</param>
/// <param name="timeProvider">Clock and timer source; defaults to <see cref="TimeProvider.System"/>.</param>
internal sealed class MessageBusTokenRefresher(
    ISession session,
    Func<Task<MessageBusTokens>> fetchTokens,
    Action<MessageBusTokens> applyTokens,
    TimeProvider? timeProvider = null)
{
    /// <summary>Maximum time before the token expires at which it is refreshed.</summary>
    internal static readonly TimeSpan MaxRefreshMargin = TimeSpan.FromMinutes(5);

    /// <summary>Minimum delay between scheduled refreshes, also used to retry a failed refresh.</summary>
    internal static readonly TimeSpan MinRefreshDelay = TimeSpan.FromSeconds(30);

    private readonly ISession _session = session ?? throw new ArgumentNullException(nameof(session));
    private readonly Func<Task<MessageBusTokens>> _fetchTokens = fetchTokens ?? throw new ArgumentNullException(nameof(fetchTokens));
    private readonly Action<MessageBusTokens> _applyTokens = applyTokens ?? throw new ArgumentNullException(nameof(applyTokens));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly Lock _timerLock = new();
    private ITimer? _timer;
    private DateTimeOffset? _lastRefresh;

    /// <summary>
    /// Schedules the first refresh based on the expiration of the token the transport was created with.
    /// </summary>
    /// <param name="securityToken">The transport's current security token.</param>
    public void Start(string securityToken)
    {
        Schedule(GetRefreshDelay(securityToken, _timeProvider.GetUtcNow()));
    }

    /// <summary>
    /// Handles an exception reported by the transport, refreshing the tokens in the background when the gateway
    /// rejected the security token, unless a refresh is already scheduled. Refreshes are throttled to one per
    /// <see cref="MinRefreshDelay"/>, so continuous reconnect failures don't flood the portal.
    /// </summary>
    /// <param name="message">The exception message reported by the transport.</param>
    public void OnTransportException(string message)
    {
        if (string.IsNullOrEmpty(message) || !message.Contains("401", StringComparison.Ordinal))
        {
            return;
        }

        if (_lastRefresh is { } lastRefresh && _timeProvider.GetUtcNow() - lastRefresh < MinRefreshDelay)
        {
            return;
        }

        _session.LogDebug("MessageBus rejected the security token.");
        _ = RefreshAsync();
    }

    /// <summary>
    /// Fetches new tokens, applies them to the transport and schedules the next refresh.
    /// A failed refresh is retried after <see cref="MinRefreshDelay"/>.
    /// Does nothing if a refresh is already scheduled or running.
    /// </summary>
    /// <returns>A task that completes when the refresh finishes; it never faults.</returns>
    public Task RefreshAsync()
    {
        lock (_timerLock)
        {
            if (_timer is not null)
            {
                _session.LogDebug("A MessageBus security token refresh is already scheduled, skipping.");
                return Task.CompletedTask;
            }
        }

        _session.LogDebug("Refreshing the MessageBus security token...");
        return RunRefreshAsync();
    }

    private async Task RunRefreshAsync()
    {
        if (!await _refreshLock.WaitAsync(0).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            MessageBusTokens tokens = await _fetchTokens().ConfigureAwait(false);
            _applyTokens(tokens);
            _lastRefresh = _timeProvider.GetUtcNow();
            _session.LogDebug("MessageBus security token refreshed.");

            Schedule(GetRefreshDelay(tokens.SecurityToken, _lastRefresh.Value));
        }
        catch (Exception ex)
        {
            _session.LogDebug($"Failed to refresh the MessageBus security token, retrying in {MinRefreshDelay.TotalSeconds} seconds: {ex.Message}");
            Schedule(MinRefreshDelay);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>
    /// Computes how long to wait before refreshing a token: shortly before it expires, by the smaller of
    /// <see cref="MaxRefreshMargin"/> and a fifth of its remaining lifetime, and never sooner than <see cref="MinRefreshDelay"/>.
    /// </summary>
    /// <param name="securityToken">The JWT to inspect.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The delay, or <c>null</c> if the token is not a JWT or has no expiration.</returns>
    internal static TimeSpan? GetRefreshDelay(string securityToken, DateTimeOffset now)
    {
        DateTime validTo;
        try
        {
            validTo = new JwtSecurityTokenHandler().ReadJwtToken(securityToken).ValidTo;
        }
        catch (ArgumentException)
        {
            return null;
        }

        if (validTo == DateTime.MinValue)
        {
            return null;
        }

        TimeSpan remaining = new DateTimeOffset(validTo, TimeSpan.Zero) - now;
        TimeSpan margin = remaining / 5 < MaxRefreshMargin ? remaining / 5 : MaxRefreshMargin;
        TimeSpan delay = remaining - margin;

        return delay > MinRefreshDelay ? delay : MinRefreshDelay;
    }

    private void Schedule(TimeSpan? delay)
    {
        lock (_timerLock)
        {
            _timer?.Dispose();
            _timer = delay is { } dueTime
                ? _timeProvider.CreateTimer(_ => _ = RunRefreshAsync(), null, dueTime, Timeout.InfiniteTimeSpan)
                : null;

            if (delay is null)
            {
                _session.LogDebug("MessageBus security token has no expiration, it will only be refreshed if the MessageBus rejects it.");
            }
        }
    }
}
