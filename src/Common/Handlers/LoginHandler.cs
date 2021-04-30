using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class LoginHandler : AbstractHandler
    {
        public LoginHandler(ISession session) : base(session) { }

        public async Task Run(string pat)
        {
            Session.LogDebug("Logging in");

            if (string.IsNullOrWhiteSpace(pat))
            {
                // configure session without a pat
                Session.ConfigureSession();

                // force a request to save the access token
                await new Foundation.BusinessOrchestration.ApplicationSettingManagement.InputObjects.GetApplicationBootInformationInput().GetApplicationBootInformationAsync(true);
            }
            else
            {
                // Configure session with the pat
                Session.ConfigureSession(accessToken: pat);
            }

            Session.LogInformation("User successfully logged in");
        }
    }
}
