using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.MessageBus.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public interface IDeploymentStatusAdapter
    {
        // Parse DeploymentStatus token into a nullable enum-like object
        object? ParseStatus(JObject content);

        // Returns true when the deployment is "started"
        bool IsStarted(object? status);

        // Returns true when the deployment is "finished". Out param indicates if it finished as failed
        bool IsFinished(object? status, out bool isFailed);
    }

    public class EnvironmentDeploymentStatusAdapter : IDeploymentStatusAdapter
    {
        public object? ParseStatus(JObject content)
        {
            var token = content["DeploymentStatus"];
            return token?.ToObject<DeploymentStatus?>();
        }

        public bool IsStarted(object? status)
        {
            if (status is DeploymentStatus ds)
            {
                return ds == DeploymentStatus.Deploying || ds == DeploymentStatus.Terminating;
            }
            return false;
        }

        public bool IsFinished(object? status, out bool isFailed)
        {
            isFailed = false;
            if (status is DeploymentStatus ds)
            {
                if (ds == DeploymentStatus.DeploymentFailed)
                {
                    isFailed = true;
                    return true;
                }
                if (ds == DeploymentStatus.DeploymentPartiallySucceeded || ds == DeploymentStatus.DeploymentSucceeded)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class AppInstallationStatusAdapter : IDeploymentStatusAdapter
    {
        public object? ParseStatus(JObject content)
        {
            var token = content["DeploymentStatus"];
            return token?.ToObject<AppInstallationStatus?>();
        }

        public bool IsStarted(object? status)
        {
            if (status is AppInstallationStatus s)
            {
                return s == AppInstallationStatus.Installing || s == AppInstallationStatus.Uninstalling;
            }
            return false;
        }

        public bool IsFinished(object? status, out bool isFailed)
        {
            isFailed = false;
            if (status is AppInstallationStatus s)
            {
                if (s == AppInstallationStatus.InstallationFailed || s == AppInstallationStatus.UninstallationFailed)
                {
                    isFailed = true;
                    return true;
                }
                if (s == AppInstallationStatus.InstallationSucceeded || s == AppInstallationStatus.UninstallationSucceeded)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class AppUninstallationStatusAdapter : IDeploymentStatusAdapter
    {
        public object? ParseStatus(JObject content)
        {
            var token = content["DeploymentStatus"];
            return token?.ToObject<AppInstallationStatus?>();
        }

        public bool IsStarted(object? status)
        {
            if (status is AppInstallationStatus s)
            {
                return s == AppInstallationStatus.Uninstalling;
            }
            return false;
        }

        public bool IsFinished(object? status, out bool isFailed)
        {
            isFailed = false;
            if (status is AppInstallationStatus s)
            {
                if (s == AppInstallationStatus.UninstallationFailed)
                {
                    isFailed = true;
                    return true;
                }
                if (s == AppInstallationStatus.UninstallationSucceeded)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class DeploymentProgressTracker(ISession session, IDeploymentStatusAdapter adapter)
    {
        private readonly ISession _session = session;
        private readonly IDeploymentStatusAdapter _adapter = adapter;

        private bool _hasStarted = false;
        private bool _hasFinished = false;
        private bool _hasFailed = false;
        private readonly string[] _loadingChars = { "|", "/", "-", "\\" };
        private const string _queuePositionMsg = "Queue Position:";
        private readonly string _pattern = $"{_queuePositionMsg} (\\d+)\\n";
        private (int left, int top)? _queuePositionCursorCoordinates = null;
        private (int left, int top) _queuePositionLoadingCursorCoordinates;
        private bool _presentLoading = false;

        public static DateTime? UtcOfLastMessageReceived { get; private set; } = null;

        public bool HasStarted => _hasStarted;
        public bool HasFinished => _hasFinished;
        public bool HasFailed => _hasFailed;

        public void ProcessDeploymentMessage(string subject, MbMessage message)
        {
            int initialTopLine;
            string msg;
            UtcOfLastMessageReceived = DateTime.UtcNow;

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