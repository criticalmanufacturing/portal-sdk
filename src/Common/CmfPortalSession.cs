using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public abstract class CmfPortalSession : ISession
    {
        private const string _cmfPortalDirName = "cmfportal";
        private const string _loginTokenFileName = ".cmfportaltoken";
        private static readonly string _loginCredentialsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _cmfPortalDirName);
        private static readonly string _loginCredentialsFilePath = Path.Combine(_loginCredentialsDir, _loginTokenFileName);

        public LogLevel LogLevel { get; protected set; } = LogLevel.Information;

        protected string AccessToken
        {
            get
            {
                // see if file exists
                if (File.Exists(_loginCredentialsFilePath))
                {
                    // try to deserialize
                    try
                    {
                        return File.ReadAllText(_loginCredentialsFilePath);
                    }
                    catch (Exception ex)
                    {
                        LogError(ex);
                    }
                }

                return null;
            }

            set
            {
                // write to file and set as hidden
                Directory.CreateDirectory(_loginCredentialsDir);
                File.WriteAllText(_loginCredentialsFilePath, value);
                File.SetAttributes(_loginCredentialsFilePath, FileAttributes.Hidden);
            }
        }

        public void ConfigureSession(string accessToken = null)
        {
            // make sure that empty/whitespace values are set as null
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // if the user provided a token, cache it
                AccessToken = accessToken;
            }

            ConfigureLBOs();
        }

        public void RestoreSession()
        {
            string accessToken = AccessToken;
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new Exception("Session not found. Have you tried to log in?");
            }

            ConfigureLBOs();
        }

        protected abstract void ConfigureLBOs();

        public abstract void LogDebug(string message);
        public abstract void LogError(string message);
        public abstract void LogError(Exception exception);
        public abstract void LogInformation(string message);
        public abstract void LogPendingMessages();
    }
}
