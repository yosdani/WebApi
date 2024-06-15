using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApi.Auth;
using WebApi.Support;
using Webdoc.Common.Response;
using Webdoc.Common.Settings;
using Webdoc.Common.Support;
using Webdoc.Library;
using Webdoc.Library.Entities;
using Webdoc.Library.Logic.Model;
using static Webdoc.Common.Request.UserRequests;
using static Webdoc.Common.Support.LanguageSupport;
namespace WebApi.Auth
{
  
    public class JwtAuthenticationService : IJwtAuthenticationService
    {
        private readonly string _key;
        private readonly Random _random;
        private const char a1 = '.', v1 = '$';

        public JwtAuthenticationService(string key)
        {
            _key = key;
            _random = new Random();
        }

        public Tuple<string, UserResponse> Authenticate(AppSettings settings, UserAuthenticate aur, out DateTime? expires, out LanguageObject message)
        {
            using (DisposableLazy<Context> context = WebApi.Support.WebRestApi.GetLazyContext(retrier: settings.DataAccess.ConnectionRetrier))
            {
                expires = null;
                UserResponse user = new NrUserLogic(context).Authenticate(aur, out message);
                if (user == null)
                    return null;
                return new Tuple<string, UserResponse>(GetToken_Username(user.UserName, out expires, user.Role.Id), user);
            }
        }

        private string? GetToken(string data, out DateTime? expires, double extendHours, int? roleId)
        {
            if (!string.IsNullOrEmpty(data))
            {
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                byte[] tokenKey = Encoding.ASCII.GetBytes(_key);
                expires = DateTime.UtcNow.AddHours(extendHours);
                List<Claim> claims = new List<Claim>() { new Claim(ClaimTypes.Email, data) };
                if (roleId != null)
                    claims.Add(new Claim(ClaimTypes.Role, roleId.Value.ToString()));
                SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = expires,
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256Signature)
                };
                SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
            }
            expires = null;
            return null;
        }

        public string RefreshToken(string token, string refreshCode, out DateTime? expires)
        {
            if (!string.IsNullOrWhiteSpace(refreshCode))
            {
                Tuple<string, JwtSecurityToken> tk = GeneralSupport.GetTokenData(token);
                if (tk != null)
                    return GetToken(tk.Item1, out expires, WebRestApi.Settings.Timers.TokenRefreshTimeHours, GeneralSupport.GetUserRoleId(tk.Item2));
            }
            expires = null;
            return string.Empty;
        }

        public string GetToken_Username(string username, out DateTime? expires, int roleId) => GetToken_Username(username, _random.Next().ToString(), out expires, roleId);

        public string? GetToken_Username(string username, string key, out DateTime? expires, int roleId)
        {
            if (!string.IsNullOrWhiteSpace(username) && RegexSupport.positiveIntegerRegex.IsMatch(key))
                return GetToken(GeneralSupport.JoinTokenData(username, key), out expires,WebRestApi.Settings.Timers.TokenLifetimeHours, roleId);
            expires = null;
            return null;
        }

        public string GetToken_GUID(out DateTime? expires) => GetToken_GUID(Guid.NewGuid().ToString(), out expires);

        public string? GetToken_GUID(string guid, out DateTime? expires) => GetToken_GUID(guid, _random.Next().ToString(), out expires);

        public string? GetToken_GUID(string guid, string key, out DateTime? expires)
        {
            if (guid != null && RegexSupport.guidRegex.IsMatch(guid) && RegexSupport.positiveIntegerRegex.IsMatch(key))
                return GetToken(GeneralSupport.JoinTokenData(guid, key), out expires, WebRestApi.Settings.Timers.TokenLifetimeHours, null);
            expires = null;
            return null;
        }

        public static string ToURLFix(string text) => text.Replace(a1, v1);

        public static string FromURLFix(string text) => text.Replace(v1, a1);
    }
}
