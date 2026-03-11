using Cmf.CustomerPortal.BusinessObjects;
using Cmf.MessageBus.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public abstract class DeploymentProgressTrackerBase
    {
        public static DateTime? UtcOfLastMessageReceived { get; protected set; }
    }
    public interface IDeploymentStatusAdapter<TStatus>
    {
        TStatus ParseStatus(JObject content);
        bool IsStarted(TStatus status);
        bool IsFinished(TStatus status, out bool isFailed);
    }

    public class EnvironmentDeploymentStatusAdapter : IDeploymentStatusAdapter<DeploymentStatus>
    {
        public DeploymentStatus ParseStatus(JObject content)
        {
            var token = content["DeploymentStatus"];
            return token?.ToObject<DeploymentStatus>() ?? throw new InvalidOperationException("DeploymentStatus missing"); ;
        }

        public bool IsStarted(DeploymentStatus status)
        {
            return status == DeploymentStatus.Deploying || status == DeploymentStatus.Terminating;
        }

        public bool IsFinished(DeploymentStatus status, out bool isFailed)
        {
            isFailed = false;
            if (status == DeploymentStatus.DeploymentFailed)
            {
                isFailed = true;
                return true;
            }
            if (status == DeploymentStatus.DeploymentPartiallySucceeded || status == DeploymentStatus.DeploymentSucceeded)
            {
                return true;
            }
            return false;
        }
    }

    public class AppInstallationStatusAdapter : IDeploymentStatusAdapter<AppInstallationStatus>
    {
        public AppInstallationStatus ParseStatus(JObject content)
        {
            var token = content["DeploymentStatus"];
            return token?.ToObject<AppInstallationStatus>() ?? throw new InvalidOperationException("DeploymentStatus missing"); ;
        }

        public bool IsStarted(AppInstallationStatus status)
        {
            return status == AppInstallationStatus.Installing || status == AppInstallationStatus.Uninstalling;
        }

        public bool IsFinished(AppInstallationStatus status, out bool isFailed)
        {
            isFailed = false;
            if (status == AppInstallationStatus.InstallationFailed || status == AppInstallationStatus.UninstallationFailed)
            {
                isFailed = true;
                return true;
            }
            if (status == AppInstallationStatus.InstallationSucceeded || status == AppInstallationStatus.UninstallationSucceeded)
            {
                return true;
            }
            return false;
        }
    }

    public class AppUninstallationStatusAdapter : IDeploymentStatusAdapter<AppInstallationStatus>
    {
        public AppInstallationStatus ParseStatus(JObject content)
        {
            var token = content["DeploymentStatus"];
            return token?.ToObject<AppInstallationStatus>() ?? throw new InvalidOperationException("DeploymentStatus missing"); ;
        }

        public bool IsStarted(AppInstallationStatus status)
        {
            return status == AppInstallationStatus.Uninstalling;
        }

        public bool IsFinished(AppInstallationStatus status, out bool isFailed)
        {
            isFailed = false;
            if (status == AppInstallationStatus.UninstallationFailed)
            {
                isFailed = true;
                return true;
            }
            if (status == AppInstallationStatus.UninstallationSucceeded)
            {
                return true;
            }
            return false;
        }
    }

    public class DeploymentProgressTracker<TStatus>(ISession session, IDeploymentStatusAdapter<TStatus> adapter) : DeploymentProgressTrackerBase
    {
        private readonly ISession _session = session;
        private readonly IDeploymentStatusAdapter<TStatus> _adapter = adapter;

        private bool _hasStarted = false;
        private bool _hasFinished = false;
        private bool _hasFailed = false;
        private readonly string[] _loadingChars = { "|", "/", "-", "\\" };
        private const string _queuePositionMsg = "Queue Position:";
        private readonly string _pattern = $"{_queuePositionMsg} (\\d+)\\n";
        private (int left, int top)? _queuePositionCursorCoordinates = null;
        private (int left, int top) _queuePositionLoadingCursorCoordinates;
        private bool _presentLoading = false;

        public bool HasStarted => _hasStarted;
        public bool HasFinished => _hasFinished;
        public bool HasFailed => _hasFailed;

        public void ProcessDeploymentMessage(string subject, MbMessage message)
        {
            int initialTopLine;
            string msg;
            DeploymentProgressTrackerBase.UtcOfLastMessageReceived = DateTime.UtcNow;

            if (message != null && !string.IsNullOrWhiteSpace(message.Data))
            {
                string jsonString = message.Data.Trim('\"');
                jsonString = jsonString.Replace("\\\"", "\"");
                var messageContentFormat = new { Data = string.Empty, DeploymentStatus = (object?)null, StepId = string.Empty };
                var contentStr = JsonConvert.DeserializeAnonymousType(jsonString, messageContentFormat);
                msg = contentStr.Data ?? string.Empty;
                JObject contentObj = JObject.Parse(jsonString);

                var statusObj = _adapter.ParseStatus(contentObj);

                Match match = Regex.Match(msg, _pattern);

                if (match.Success && !_hasStarted)
                {
                    string position = match.Groups[1].Value;

                    if (!string.IsNullOrWhiteSpace(position))
                    {
                        try
                        {
                            if (_queuePositionCursorCoordinates == null)
                            {
                                _queuePositionCursorCoordinates = Console.GetCursorPosition();
                            }

                            initialTopLine = Console.CursorTop;
                            msg = $"{_queuePositionMsg} {position}";
                            Console.SetCursorPosition(_queuePositionCursorCoordinates.Value.left, _queuePositionCursorCoordinates.Value.top - 1);
                            _session.LogInformation(msg);

                            _queuePositionLoadingCursorCoordinates = (_queuePositionCursorCoordinates.Value.left + msg.Length, _queuePositionCursorCoordinates.Value.top - 1);
                            Console.SetCursorPosition(0, initialTopLine);

                            _presentLoading = true;
                        }
                        catch (Exception)
                        {
                            _session.LogInformation($"{_queuePositionMsg} {position}");
                            _presentLoading = true;
                        }
                    }
                }

                if (!msg.StartsWith(_queuePositionMsg))
                {
                    _session.LogInformation(msg);
                }

                if (_adapter.IsStarted(statusObj))
                {
                    _presentLoading = false;
                    _hasStarted = true;
                }

                if (_adapter.IsFinished(statusObj, out bool isFailed))
                {
                    if (isFailed)
                    {
                        _hasFailed = true;
                    }
                    _hasFinished = true;
                    _hasStarted = true;
                }
            }
            else
            {
                _session.LogInformation("Unknown message received");
            }
        }

        public async Task ShowLoadingIndicator(CancellationToken token)
        {
            int loadingIndex = 0;
            (int left, int top) initialPosition;
            while (!_hasStarted && !token.IsCancellationRequested)
            {
                try
                {
                    if (_presentLoading)
                    {
                        initialPosition = Console.GetCursorPosition();
                        Console.SetCursorPosition(_queuePositionLoadingCursorCoordinates.left, _queuePositionLoadingCursorCoordinates.top);
                        Console.Write($" {_loadingChars[loadingIndex]} {new string(' ', Console.WindowWidth)}");
                        loadingIndex = (loadingIndex + 1) % _loadingChars.Length;
                        Console.SetCursorPosition(initialPosition.left, initialPosition.top);
                    }
                }
                catch { }

                await Task.Delay(500);
            }
        }
    }
}