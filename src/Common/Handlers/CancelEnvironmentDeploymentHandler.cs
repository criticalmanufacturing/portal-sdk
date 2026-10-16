using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Extensions;
using Cmf.CustomerPortal.Sdk.Common.Services;
using System;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers;

/// <summary>
/// Cancels the in-progress deployment of a Customer Environment.
/// </summary>
public class CancelEnvironmentDeploymentHandler(
    ISession session,
    ICustomerEnvironmentServices customerEnvironmentServices,
    ICustomerPortalClient customerPortalClient) : AbstractHandler(session, true)
{
    /// <summary>
    /// Cancels the deployment of the Customer Environment named <paramref name="name"/>.
    /// </summary>
    /// <param name="name">Name of the Customer Environment.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="name"/> is empty.</exception>
    /// <exception cref="Exception">When the Customer Environment does not exist.</exception>
    public async Task Run(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        await EnsureLogin();

        Session.LogInformation($"Checking if customer environment {name} exists...");
        CustomerEnvironment environment = await customerEnvironmentServices.GetCustomerEnvironment(name);
        if (environment == null)
        {
            throw new Exception($"Customer environment with name '{name}' does not exist...");
        }

        Session.LogInformation($"Cancelling the deployment of customer environment {name} {environment.DescribeVersion()}...");
        await customerPortalClient.CancelEnvironmentDeployment(environment.Id);

        Session.LogInformation($"Customer environment {name} deployment cancel requested successfully.");
        Utils.LogCustomerEnvironmentUrl("Follow the cancellation at", Session, environment);
    }
}
