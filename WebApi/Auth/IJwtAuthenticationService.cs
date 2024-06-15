using Webdoc.Common.Response;
using static Webdoc.Common.Request.UserRequests;
using static Webdoc.Common.Support.LanguageSupport;

namespace WebApi.Auth
{
    public interface IJwtAuthenticationService
    {
        Tuple<string, UserResponse> Authenticate(Webdoc.Common.Settings.AppSettings settings, UserAuthenticate aur, out DateTime? expires, out LanguageObject message);

        //string GetToken_Email(string email, out DateTime? expires, int roleId);

        string GetToken_GUID(out DateTime? expires);

        string RefreshToken(string token, string refreshCode, out DateTime? expires);
    }
}
