using Cmf.CustomerPortal.BusinessObjects;
using Cmf.MessageBus.Messages;
using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services;

public interface IDeploymentProgressTracker
{
    bool HasStarted { get; }
    bool HasFinished { get; }
    bool HasFailed { get; }

    void ProcessDeploymentMessage(string subject, MbMessage message);

    Task ShowLoadingIndicator(CancellationToken token);
}
    
public interface IDeploymentProgressTrackerFactory
{
    IDeploymentProgressTracker CreateAppUninstallationTracker();

    IDeploymentProgressTracker CreateAppInstallationTracker();

    IDeploymentProgressTracker CreateEnvironmentDeploymentTracker();
}
    
public class DeploymentProgressTrackerFactory(ISession session) : IDeploymentProgressTrackerFactory
{
    public IDeploymentProgressTracker CreateAppUninstallationTracker()
    {
        return new DeploymentProgressTracker<AppInstallationStatus>(
            session,
            new AppUninstallationStatusAdapter()
        );
    }

    public IDeploymentProgressTracker CreateAppInstallationTracker()
    {
        return new DeploymentProgressTracker<AppInstallationStatus>(
            session,
            new AppInstallationStatusAdapter()
        );
    }

    public IDeploymentProgressTracker CreateEnvironmentDeploymentTracker()
    {
        return new DeploymentProgressTracker<DeploymentStatus>(
            session,
            new EnvironmentDeploymentStatusAdapter()
        );
    }
}
    
public abstract class DeploymentProgressTrackerBase
{
    public static DateTime? UtcOfLastMessageReceived { get; protected set; }
}
public interface IDeploymentStatusAdapter<in TStatus>
{
    bool IsStarted(TStatus status);
    bool IsFinished(TStatus status, out bool isFailed);
}

public class EnvironmentDeploymentStatusAdapter : IDeploymentStatusAdapter<DeploymentStatus>
{
    public bool IsStarted(DeploymentStatus status)
    {
        return status is DeploymentStatus.Deploying or DeploymentStatus.Terminating;
    }

    public bool IsFinished(DeploymentStatus status, out bool isFailed)
    {
        isFailed = false;
        if (status is DeploymentStatus.DeploymentFailed)
        {
            isFailed = true;
            return true;
        }
        if (status is DeploymentStatus.DeploymentPartiallySucceeded or DeploymentStatus.DeploymentSucceeded)
        {
            return true;
        }
        return false;
    }
}

public class AppInstallationStatusAdapter : IDeploymentStatusAdapter<AppInstallationStatus>
{
    public bool IsStarted(AppInstallationStatus status)
    {
        return status is AppInstallationStatus.Installing or AppInstallationStatus.Uninstalling;
    }

    public bool IsFinished(AppInstallationStatus status, out bool isFailed)
    {
        isFailed = false;
        switch (status)
        {
            case AppInstallationStatus.InstallationFailed:
            case AppInstallationStatus.UninstallationFailed:
                isFailed = true;
                return true;
            case AppInstallationStatus.InstallationSucceeded:
            case AppInstallationStatus.UninstallationSucceeded:
                return true;
            default:
                return false;
        }
    }
}

public class AppUninstallationStatusAdapter : IDeploymentStatusAdapter<AppInstallationStatus>
{
    public bool IsStarted(AppInstallationStatus status)
    {
        return status is AppInstallationStatus.Uninstalling;
    }

    public bool IsFinished(AppInstallationStatus status, out bool isFailed)
    {
        isFailed = false;
        switch (status)
        {
            case AppInstallationStatus.UninstallationFailed:
                isFailed = true;
                return true;
            case AppInstallationStatus.UninstallationSucceeded:
                return true;
            default:
                return false;
        }
    }
}

public partial class DeploymentProgressTracker<TStatus>(ISession session, IDeploymentStatusAdapter<TStatus> adapter) : DeploymentProgressTrackerBase, IDeploymentProgressTracker
{
    private readonly string[] _loadingChars = ["|", "/", "-", "\\"];
    private const string _queuePositionMsg = "Queue Position:";
    private (int left, int top)? _queuePositionCursorCoordinates;
    private (int left, int top) _queuePositionLoadingCursorCoordinates;
    private bool _presentLoading;
    
    [GeneratedRegex($@"{_queuePositionMsg} (\d+)\n")]
    private static partial Regex QueuePositionRegex();

    public bool HasStarted { get; private set; }

    public bool HasFinished { get; private set; }

    public bool HasFailed { get; private set; }
    
    
    public void ProcessDeploymentMessage(string subject, MbMessage message)
    {
        UtcOfLastMessageReceived = DateTime.UtcNow;

        if (message != null && !string.IsNullOrWhiteSpace(message.Data))
        {
            string jsonString = message.Data.Trim('\"').Replace("\\\"", "\"");
            
            using JsonDocument contentDocument = JsonDocument.Parse(jsonString);
            var root = contentDocument.RootElement;

            var msg = root.TryGetProperty("Data", out var dataToken)
                ? dataToken.GetString() ?? string.Empty
                : string.Empty;

            if (!root.TryGetProperty("DeploymentStatus", out var statusElement))
            {
                throw new InvalidOperationException("DeploymentStatus missing");
            }

            var statusObj = statusElement.Deserialize<TStatus>();
            if (statusObj is null)
            {
                throw new InvalidOperationException("DeploymentStatus is null");
            }
            
            var match = QueuePositionRegex().Match(msg);
            if (match.Success && !HasStarted)
            {
                var position = match.Groups[1].Value;

                if (!string.IsNullOrWhiteSpace(position))
                {
                    try
                    {
                        _queuePositionCursorCoordinates ??= Console.GetCursorPosition();

                        msg = $"{_queuePositionMsg} {position}";
                        Console.SetCursorPosition(_queuePositionCursorCoordinates.Value.left, _queuePositionCursorCoordinates.Value.top - 1);
                        session.LogInformation(msg);

                        _queuePositionLoadingCursorCoordinates = (_queuePositionCursorCoordinates.Value.left + msg.Length, _queuePositionCursorCoordinates.Value.top - 1);
                        Console.SetCursorPosition(0, Console.CursorTop);

                        _presentLoading = true;
                    }
                    catch (Exception)
                    {
                        session.LogInformation($"{_queuePositionMsg} {position}");
                        _presentLoading = true;
                    }
                }
            }

            if (!msg.StartsWith(_queuePositionMsg))
            {
                session.LogInformation(msg);
            }

            if (adapter.IsStarted(statusObj))
            {
                _presentLoading = false;
                HasStarted = true;
            }

            if (adapter.IsFinished(statusObj, out bool isFailed))
            {
                if (isFailed)
                {
                    HasFailed = true;
                }
                HasFinished = true;
                HasStarted = true;
            }
        }
        else
        {
            session.LogInformation("Unknown message received");
        }
    }

    public async Task ShowLoadingIndicator(CancellationToken token)
    {
        int loadingIndex = 0;
        while (!HasStarted && !token.IsCancellationRequested)
        {
            try
            {
                if (_presentLoading)
                {
                    (int left, int top) initialPosition = Console.GetCursorPosition();
                    Console.SetCursorPosition(_queuePositionLoadingCursorCoordinates.left, _queuePositionLoadingCursorCoordinates.top);
                    Console.Write($" {_loadingChars[loadingIndex]} {new string(' ', Console.WindowWidth)}");
                    loadingIndex = (loadingIndex + 1) % _loadingChars.Length;
                    Console.SetCursorPosition(initialPosition.left, initialPosition.top);
                }
            }
            catch { }

            await Task.Delay(500, token);
        }
    }
}
