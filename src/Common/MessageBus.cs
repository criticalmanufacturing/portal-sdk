using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cmf.Foundation.BusinessOrchestration.ApplicationSettingManagement.InputObjects;
using Cmf.MessageBus.Client;

namespace Cmf.CustomerPortal.Sdk.Common;

public interface IMessageBusTransport
{
    void Subscribe(string subject, OnMbMessageCallback handler);
}

public class MessageBusTransportAdapter(Transport transport) : IMessageBusTransport
{
    public void Subscribe(string subject, OnMbMessageCallback handler)
    {
        transport.Subscribe(subject, handler);
    }
}

public partial class CustomerPortalClient
{
    private readonly SemaphoreSlim _transportLock = new(1, 1);
    private const string jwtTenantNameKey = "tenantName";

    private IMessageBusTransport? _transport;


    public async Task<IMessageBusTransport> GetMessageBusTransport()
    {
        if (_transport != null)
        {
            return _transport;
        }

        if (!await _transportLock.WaitAsync(TimeSpan.FromSeconds(30)))
        {
            throw new TimeoutException("Timed out waiting to configure MessageBus transport.");
        }

        try
        {
            if (_transport != null)
            {
                return _transport;
            }

            session.LogDebug($"Configuring message bus...");

            // create new transport using the config
            var applicationBootInformation =
                await new GetApplicationBootInformationInput().GetApplicationBootInformationAsync(true);
            var transportConfig = JsonSerializer.Deserialize<TransportConfig>(applicationBootInformation.TransportConfig);
            if (transportConfig == null)
            {
                throw new InvalidOperationException("Failed to deserialize MessageBus transport configuration.");
            }
            
            transportConfig.ApplicationName = "Customer Portal Client (PortalSDK)";
            transportConfig.TenantName = new JwtSecurityTokenHandler()
                .ReadJwtToken(applicationBootInformation.MessageBusToken).Payload[jwtTenantNameKey].ToString();
            transportConfig.SecurityToken = applicationBootInformation.MessageBusToken;
            Transport messageBus = new Transport(transportConfig);
            messageBus.SetDataGroupToken(applicationBootInformation.MessageBusDataGroupsToken);

            // Register events
            messageBus.Connected += () => { session.LogDebug("Message Bus Connect!"); };

            messageBus.Disconnected += () => { session.LogDebug("Message Bus Disconnected!"); };

            messageBus.InformationMessage += session.LogDebug;

            messageBus.Exception += message => { session.LogError(new Exception(message)); };

            // start the message bus and ensure that it is connected
            messageBus.Start();

            bool failedConnection = false;
            TimeSpan timeout = TimeSpan.FromSeconds(15);
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeout))
            {
                failedConnection = await Task.Run(
                    async () =>
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
                    }
                );
            }

            // if we failed to setup the message bus, throw error
            if (failedConnection)
            {
                Exception error = new Exception("Timed out waiting for client to connect to MessageBus");
                session.LogError(error);
                throw error;
            }

            session.LogDebug("Message Bus connected with success!");

            _transport = new MessageBusTransportAdapter(messageBus);
        }
        finally
        {
            _transportLock.Release();
        }

        return _transport;
    }
}
