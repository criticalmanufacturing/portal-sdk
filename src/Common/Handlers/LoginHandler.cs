using System;
using System.Buffers.Text;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Cmf.Foundation.BusinessOrchestration.ApplicationSettingManagement.InputObjects;
using Microsoft.Extensions.Configuration;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers;

/// <summary>
/// Logs in to the Customer Portal.
/// </summary>
public class LoginHandler(
    ISession session,
    ICustomerPortalClient customerPortalClient
) : AbstractHandler(session, false)
{
    /// <summary>
    /// Logs in with a Personal Access Token, or in the local browser, and saves the token for later commands.
    /// </summary>
    /// <param name="pat">Personal Access Token, or <c>null</c> to log in interactively.</param>
    public async Task Run(string pat)
    {
        await EnsureLogin();

        Session.LogDebug("Logging in");

        if (string.IsNullOrWhiteSpace(pat))
        {
            // configure session without a pat
            Session.ConfigureSession();

            // force a request to save the access token
            await new GetApplicationBootInformationInput().GetApplicationBootInformationAsync(true);
        }
        else
        {
            // Configure session with the pat
            Session.ConfigureSession(token: pat);
        }

        Session.LogInformation("User successfully logged in");
    }

    /// <summary>
    /// Signs in through a browser that may run on another machine, and returns the refresh token instead of saving it.
    /// </summary>
    /// <param name="redirectUri">URI the Security Portal sends the browser back to after the sign-in.</param>
    /// <param name="waitForCallback">
    /// Shows the sign-in URL it is given and returns the callback URL received at <paramref name="redirectUri"/>,
    /// or <c>null</c> when the sign-in was cancelled.
    /// </param>
    /// <returns>The refresh token of the signed-in user.</returns>
    public async Task<string> RunWithRedirect(Uri redirectUri, Func<Uri, Task<string?>> waitForCallback)
    {
        ArgumentNullException.ThrowIfNull(redirectUri);
        ArgumentNullException.ThrowIfNull(waitForCallback);

        IConfiguration configuration = Session.Configuration;
        string clientId = configuration["ClientConfiguration:ClientId"]!;
        Uri userEndpoint = new(
            new Uri(new Uri(configuration["ClientConfiguration:SecurityPortalBaseAddress"]!), "tenant/"),
            $"{configuration["ClientConfiguration:ClientTenantName"]}/"
        );

        string codeVerifier = RandomBase64Url();
        string state = RandomBase64Url();
        NameValueCollection query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = clientId;
        query["redirect_uri"] = redirectUri.AbsoluteUri;
        query["response_type"] = "code";
        query["state"] = state;
        query["code_challenge"] = Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
        query["code_challenge_method"] = "S256";
        Uri authorizationUrl = new UriBuilder(new Uri(userEndpoint, "login")) { Query = query.ToString() }.Uri;

        Session.LogDebug("Waiting for the callback URL");
        string callback = await waitForCallback(authorizationUrl)
            ?? throw new InvalidOperationException("The sign-in was cancelled: no callback URL was received.");

        if (!Uri.TryCreate(callback.Trim(), UriKind.Absolute, out Uri? callbackUrl))
        {
            throw new InvalidOperationException("The callback is not an absolute URL.");
        }

        NameValueCollection callbackQuery = HttpUtility.ParseQueryString(callbackUrl.Query);
        if (callbackQuery["error"] is string error)
        {
            throw new InvalidOperationException($"The sign-in failed: {error}.");
        }

        if (callbackQuery["state"] != state)
        {
            throw new InvalidOperationException("The callback URL does not belong to this sign-in.");
        }

        string? code = callbackQuery["code"];
        if (string.IsNullOrEmpty(code))
        {
            throw new InvalidOperationException("The callback URL has no authorization code.");
        }

        // Gives the LBO HTTP client its configuration. Without a token nothing is saved, and no LBO sign-in runs here.
        Session.ConfigureSession();
        string? refreshToken = await customerPortalClient.ExchangeAuthorizationCode(
            userEndpoint,
            clientId,
            code,
            codeVerifier
        );
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new InvalidOperationException("The Security Portal did not return a refresh token.");
        }

        Session.LogDebug("User successfully logged in");
        return refreshToken;
    }

    private static string RandomBase64Url() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
}
