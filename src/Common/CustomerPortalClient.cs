using Cmf.Foundation.BusinessOrchestration.ApplicationSettingManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.Foundation.Common.Base;
using Cmf.LightBusinessObjects.Infrastructure;
using Cmf.MessageBus.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public class CustomerPortalClient : ICustomerPortalClient
    {
        private static readonly SemaphoreSlim _transportLock = new SemaphoreSlim(1, 1);

        private readonly ISession _session;
        private Transport _transport;

        public CustomerPortalClient(ISession session)
        {
            _session = session;
        }

        /// <summary>
        /// Gets object by Id
        /// </summary>
        /// <typeparam name="T">The object type</typeparam>
        /// <param name="name">The object name</param>
        /// <param name="levelsToLoad">Levels to Load</param>
        /// <returns></returns>
        public async Task<T> GetObjectByName<T>(string name, int levelsToLoad = 0) where T : CoreBase, new()
        {
            var output = await new GetObjectByNameInput
            {
                Name = name,
                Type = typeof(T).BaseType.Name == typeof(CoreBase).Name ? new T() : (object)new T().GetType().Name,
                LevelsToLoad = levelsToLoad
            }.GetObjectByNameAsync(true);

            return output.Instance as T;
        }

        public async Task<T> LoadObjectRelations<T>(T obj, System.Collections.ObjectModel.Collection<string> relationsNames) where T : CoreBase, new()
        {
            return (await new LoadObjectRelationsInput
            {
                Object = obj,
                RelationNames = relationsNames
            }.LoadObjectRelationsAsync(true)).Object as T;
        }

        /// <summary>
        /// Setup Message Bus Transport
        /// </summary>
        /// <returns>An instance of Message Bus transport</returns>
        public async Task<Transport> GetMessageBusTransport()
        {
            if (_transport != null)
            {
                return _transport;
            }

            await _transportLock.WaitAsync(TimeSpan.FromSeconds(30));

            try
            {
                if (_transport != null)
                {
                    return _transport;
                }

                _session.LogDebug($"Configuring message bus...");

                // create new transport using the config
                string transportConfigString = (await new GetApplicationBootInformationInput().GetApplicationBootInformationAsync(true)).TransportConfig;
                TransportConfig transportConfig = JsonConvert.DeserializeObject<TransportConfig>(transportConfigString);
                transportConfig.ApplicationName = "Customer Portal Client";
                transportConfig.TenantName = ClientConfigurationProvider.ClientConfiguration.ClientTenantName;
                Transport messageBus = new Transport(transportConfig);

                // Register events
                messageBus.Connected += () =>
                {
                    _session.LogDebug("Message Bus Connect!");
                };

                messageBus.Disconnected += () =>
                {
                    _session.LogDebug("Message Bus Disconnected!");
                };

                messageBus.InformationMessage += (string message) =>
                {
                    _session.LogDebug(message);
                };

                messageBus.Exception += (string message) =>
                {
                    _session.LogError(new Exception(message));
                };

                // start the message bus and ensure that it is connected
                messageBus.Start();

                bool failedConnection = false;
                TimeSpan timeout = TimeSpan.FromSeconds(2);
                using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeout))
                {
                    failedConnection = await Task.Run(async () =>
                    {
                        while (!messageBus.IsConnected)
                        {
                            try
                            {
                                await Task.Delay(TimeSpan.FromSeconds(0.1), cancellationTokenSource.Token);
                            }
                            catch (TaskCanceledException)
                            {
                                return true;
                            }
                        }
                        return false;
                    });
                }

                // if we failed to setup the message bus, throw error
                if (failedConnection)
                {
                    Exception error = new Exception("Timed out waiting for client to connect to MessageBus");
                    _session.LogError(error);
                    throw error;
                }

                _session.LogDebug("Message Bus connected with success!");

                _transport = messageBus;
            }
            finally
            {
                _transportLock.Release();
            }

            return _transport;
        }

        private async Task<Dictionary<string, string>> ConfigureTokens(string[] replaceTokens)
        {
            return await Task.Run(() =>
            {
                Dictionary<string, string> tokens = new Dictionary<string, string>();
                foreach (string tokenToSet in replaceTokens)
                {
                    int splitCharIndex = tokenToSet.IndexOf('=');
                    if (splitCharIndex > 0 && splitCharIndex < tokenToSet.Length - 1)
                    {
                        string[] tokenNameAndValue = tokenToSet.Split(new char[] { '=' }, 2);
                        _session.LogDebug($"Registering token {tokenNameAndValue[0]} with value {tokenNameAndValue[1]}");
                        tokens.Add(tokenNameAndValue[0], tokenNameAndValue[1]);
                    }
                }
                return tokens;
            });
        }

        private static readonly Regex REPLACE_TOKENS_REGEX = new Regex(@"\#{(.+?)}#", RegexOptions.Compiled);

        public async Task<string> ReplaceTokens(string content, string[] replaceTokens, bool isJson = false)
        {
            return await Task.Run(async () =>
            {
                StringBuilder stringBuilder = new StringBuilder(content);

                if (replaceTokens?.Length > 0)
                {
                    _session.LogDebug($"Replacing tokens");

                    Dictionary<string, string> tokens = await ConfigureTokens(replaceTokens);

                    MatchCollection m = REPLACE_TOKENS_REGEX.Matches(content);
                    int indexChanges = 0;
                    for (int i = 0; i < m.Count; i++)
                    {
                        // get env var name from match
                        string matchedString = m[i].Groups[1].Value;
                        if (!string.IsNullOrWhiteSpace(matchedString))
                        {
                            // get env var value
                            tokens.TryGetValue(matchedString, out string envVar);
                            string value = envVar ?? string.Empty;
                            if (isJson)
                            {
                                // escape backslashes
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    value = value.ToString().Replace("\\", "\\\\");
                                }
                                // if not inside double quotes and value is null or whitespace, be sure to add double quotes as the value or else it will result in invalid json
                                else if (!m[i].Groups[0].Value.StartsWith("\"") && !m[i].Groups[0].Value.EndsWith("\""))
                                {
                                    value = "\"\"";
                                }
                            }

                            // replace env var with ${} for the env var value
                            string envToSubstitute = m[i].Groups[0].Value;
                            if (string.IsNullOrWhiteSpace(value))
                            {
                                _session.LogInformation($"Found no match for token '{envToSubstitute}'. Replacing with '{value}'");
                            }
                            else
                            {
                                _session.LogDebug($"Replacing '{envToSubstitute}' with '{value}'");
                            }
                            stringBuilder.Replace(envToSubstitute, value, m[i].Groups[0].Index - indexChanges, m[i].Groups[0].Length);

                            indexChanges += envToSubstitute.Length - value.Length;
                        }
                    }
                }

                return stringBuilder.ToString();
            });
        }
    }
}
