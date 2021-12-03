using Cmf.Foundation.BusinessOrchestration.ApplicationSettingManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.Foundation.Common.Base;
using Cmf.LightBusinessObjects.Infrastructure;
using Cmf.MessageBus.Client;
using Newtonsoft.Json;
using System;
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
                TransportConfig transportConfig = JsonConvert.DeserializeObject<TransportConfig>((await new GetApplicationBootInformationInput().GetApplicationBootInformationAsync(true)).TransportConfig);
                transportConfig.ApplicationName = "Customer Portal Client";
                transportConfig.TenantName = ClientConfigurationProvider.ClientConfiguration.ClientTenantName;
                transportConfig.SecurityToken = transportConfig.SecurityToken == null ? _session.AccessToken : transportConfig.SecurityToken;
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
    }
}
