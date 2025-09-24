using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class TokenHandler : AbstractHandler
    {
        public TokenHandler(ISession session) : base(session, true) { }

        public async override Task Run()
        {
            await EnsureLogin();

            Session.PrintSessionToken();
        }
    }
}
