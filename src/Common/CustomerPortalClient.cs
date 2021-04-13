using Cmf.Foundation.BusinessOrchestration.ApplicationSettingManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.Foundation.Common.Base;
using Cmf.LightBusinessObjects.Infrastructure;
using Cmf.MessageBus.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public class CustomerPortalClient : ICustomerPortalClient
    {
        private static SemaphoreSlim transportLock = new SemaphoreSlim(1, 1);

        private readonly ISession session;
        private Transport transport;

        public CustomerPortalClient(ISession session)
        {
            this.session = session;
        }

        /// <summary>
        /// Gets object by Id
        /// </summary>
        /// <typeparam name="T">The object type</typeparam>
        /// <param name="name">The object name</param>
        /// <param name="levelsToLoad">Levels to Load</param>
        /// <returns></returns>
        public async Task<T> GetObjectByName<T>(String name, Int32 levelsToLoad = 0) where T : CoreBase, new()
        {
            var output = await new GetObjectByNameInput
            {
                Name = name,
                Type = typeof(T).BaseType.Name == typeof(CoreBase).Name ? new T() : (object)new T().GetType().Name,
                LevelsToLoad = levelsToLoad
            }.GetObjectByNameAsync();

            return output.Instance as T;
        }

        /// <summary>
        /// Setup Message Bus Transport
        /// </summary>
        /// <returns>An instance of Message Bus transport</returns>
        public async Task<Transport> GetMessageBusTransport()
        {
            if (transport != null)
            {
                return transport;
            }

            await transportLock.WaitAsync(30 * 1000);

            try
            {
                if (transport != null)
                {
                    return transport;
                }

                session.LogDebug($"Configuring message bus...");

                var transportConfigString = (await new GetApplicationBootInformationInput().GetApplicationBootInformationAsync()).TransportConfig;
                var transportConfig = JsonConvert.DeserializeObject<TransportConfig>(transportConfigString);

                transportConfig.ApplicationName = "Customer Portal Client";
                transportConfig.TenantName = ClientConfigurationProvider.ClientConfiguration.ClientTenantName;

                var messageBus = new Transport(transportConfig);

                // Register events
                messageBus.Connected += () =>
                {
                    session.LogDebug("Message Bus Connect!");
                };

                messageBus.Disconnected += () =>
                {
                    session.LogDebug("Message Bus Disconnected!");
                };

                messageBus.InformationMessage += (string message) =>
                {
                    session.LogDebug(message);
                };

                messageBus.Exception += (string message) =>
                {
                    session.LogError(new Exception(message));
                };

                messageBus.Start();

                var timeout = 2000;
                int totalWaitedTime = 0;
                var failedConnection = false;

                while (!messageBus.IsConnected && totalWaitedTime < timeout)
                {
                    await Task.Delay(100);
                    totalWaitedTime += 100;
                }

                if (totalWaitedTime > 0 && totalWaitedTime > timeout)
                {
                    failedConnection = true;
                }


                if (failedConnection)
                {
                    var error = new Exception("Timed out waiting for client to connect to MessageBus");
                    session.LogError(error);
                    throw error;
                }

                session.LogInformation("Message Bus connected with sucess!");

                transport = messageBus;

            }
            finally
            {
                transportLock.Release();
            }


            return transport;

        }
    }
}
