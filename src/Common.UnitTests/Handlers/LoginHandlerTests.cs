using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Moq;

namespace Common.UnitTests.Handlers;

public class LoginHandlerTests
{
    private static readonly Uri RedirectUri = new("https://service.example.com/callback");
    private static readonly Uri UserEndpoint = new("https://security.example.com/tenant/Tenant/");

    private readonly Mock<ISession> _sessionMock = new();
    private readonly Mock<ICustomerPortalClient> _customerPortalClientMock = new();

    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _sessionMock.SetupGet(s => s.Configuration).Returns(PortalConfiguration.Create());
        _handler = new LoginHandler(_sessionMock.Object, _customerPortalClientMock.Object);
    }

    [Fact]
    public async Task RunWithRedirect_ExchangesTheCodeOfTheCallbackAndReturnsTheRefreshToken()
    {
        // Arrange
        Uri? authorizationUrl = null;
        string? codeVerifier = null;
        _customerPortalClientMock
            .Setup(c => c.ExchangeAuthorizationCode(UserEndpoint, "Applications", "the-code", It.IsAny<string>()))
            .Callback((Uri _, string _, string _, string verifier) => codeVerifier = verifier)
            .ReturnsAsync("refresh-token");

        // Act
        string refreshToken = await _handler.RunWithRedirect(RedirectUri, url =>
        {
            authorizationUrl = url;
            return Callback("?code=the-code&state={state}")(url);
        });

        // Assert
        Assert.Equal("refresh-token", refreshToken);
        // a session configured with a token saves it to the token file
        _sessionMock.Verify(s => s.ConfigureSession(It.IsNotNull<string>()), Times.Never);
        Assert.NotNull(authorizationUrl);
        Assert.Equal("https://security.example.com/tenant/Tenant/login", authorizationUrl.GetLeftPart(UriPartial.Path));
        var query = Query(authorizationUrl);
        Assert.Equal("Applications", query["client_id"]);
        Assert.Equal(RedirectUri.AbsoluteUri, query["redirect_uri"]);
        Assert.Equal("code", query["response_type"]);
        Assert.False(string.IsNullOrEmpty(query["state"]));
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.NotNull(codeVerifier);
        // RFC 7636: BASE64URL(SHA256(ASCII(code_verifier)))
        string expectedChallenge = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        Assert.Equal(expectedChallenge, query["code_challenge"]);
    }

    [Fact]
    public async Task RunWithRedirect_WhenTheCallbackStateDoesNotMatch_ThrowsWithoutExchangingTheCode()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.RunWithRedirect(RedirectUri, Callback("?code=the-code&state=other"))
        );
        VerifyNoExchange();
    }

    [Fact]
    public async Task RunWithRedirect_WhenTheCallbackHasAnError_ThrowsWithTheErrorWithoutExchangingTheCode()
    {
        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.RunWithRedirect(RedirectUri, Callback("?error=access_denied&state={state}"))
        );

        // Assert
        Assert.Contains("access_denied", exception.Message);
        VerifyNoExchange();
    }

    [Theory]
    [InlineData("?state={state}")]
    [InlineData("?code=&state={state}")]
    public async Task RunWithRedirect_WhenTheCallbackHasNoCode_ThrowsWithoutExchangingTheCode(string query)
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.RunWithRedirect(RedirectUri, Callback(query))
        );
        VerifyNoExchange();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not a url")]
    public async Task RunWithRedirect_WhenNoCallbackUrlIsReceived_ThrowsWithoutExchangingTheCode(string? callback)
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.RunWithRedirect(RedirectUri, _ => Task.FromResult(callback))
        );
        VerifyNoExchange();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task RunWithRedirect_WhenTheExchangeReturnsNoRefreshToken_Throws(string? refreshToken)
    {
        // Arrange
        _customerPortalClientMock
            .Setup(c => c.ExchangeAuthorizationCode(UserEndpoint, "Applications", "the-code", It.IsAny<string>()))
            .ReturnsAsync(refreshToken);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.RunWithRedirect(RedirectUri, Callback("?code=the-code&state={state}"))
        );
    }

    [Fact]
    public async Task RunWithRedirect_WhenAnArgumentIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.RunWithRedirect(null!, Callback("")));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.RunWithRedirect(RedirectUri, null!));
    }

    /// <summary>
    /// Returns the redirect URI with <paramref name="query"/>, where <c>{state}</c> is the state of the sign-in URL.
    /// </summary>
    private static Func<Uri, Task<string?>> Callback(string query) =>
        url => Task.FromResult<string?>(RedirectUri + query.Replace("{state}", Query(url)["state"]));

    private void VerifyNoExchange() => _customerPortalClientMock.Verify(
        c => c.ExchangeAuthorizationCode(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
        Times.Never
    );

    private static NameValueCollection Query(Uri url) => HttpUtility.ParseQueryString(url.Query);
}
