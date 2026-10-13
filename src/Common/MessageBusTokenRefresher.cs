using System;
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
/// whenever the transport reports a 401, so the next reconnect succeeds.
/// </summary>
/// <param name="session">Session used for logging.</param>
/// <param name="fetchTokens">Fetches new tokens from the portal.</param>
/// <param name="applyTokens">Applies the new tokens to the transport.</param>
/// <param name="timeProvider">Clock used to throttle refreshes; defaults to <see cref="TimeProvider.System"/>.</param>
internal sealed class MessageBusTokenRefresher(
    ISession session,
    Func<Task<MessageBusTokens>> fetchTokens,
    Action<MessageBusTokens> applyTokens,
    TimeProvider? timeProvider = null)
{
    /// <summary>Minimum time between refresh attempts, so continuous reconnect failures don't flood the portal.</summary>
    internal static readonly TimeSpan MinRefreshInterval = TimeSpan.FromSeconds(30);

    private readonly ISession _session = session ?? throw new ArgumentNullException(nameof(session));
    private readonly Func<Task<MessageBusTokens>> _fetchTokens = fetchTokens ?? throw new ArgumentNullException(nameof(fetchTokens));
    private readonly Action<MessageBusTokens> _applyTokens = applyTokens ?? throw new ArgumentNullException(nameof(applyTokens));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private DateTimeOffset? _lastAttempt;

    /// <summary>
    /// Handles an exception reported by the transport, refreshing the tokens in the background when the gateway
    /// rejected the security token. A refresh is attempted at most once per <see cref="MinRefreshInterval"/>,
    /// and a failed one is retried on the next rejection after that.
    /// </summary>
    /// <param name="message">The exception message reported by the transport.</param>
    public void OnTransportException(string message)
    {
        if (string.IsNullOrEmpty(message) || !message.Contains("401", StringComparison.Ordinal))
        {
            return;
        }

        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (!await _refreshLock.WaitAsync(0).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            if (_lastAttempt is { } lastAttempt && now - lastAttempt < MinRefreshInterval)
            {
                return;
            }

            _lastAttempt = now;
            _session.LogDebug("MessageBus rejected the security token, refreshing it...");

            MessageBusTokens tokens = await _fetchTokens().ConfigureAwait(false);
            _applyTokens(tokens);
            _session.LogDebug("MessageBus security token refreshed.");
        }
        catch (Exception ex)
        {
            _session.LogDebug($"Failed to refresh the MessageBus security token, it will be retried on the next rejection: {ex.Message}");
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
