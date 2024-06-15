using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Webdoc.Common.Response;
using Webdoc.Common.Support;
using Webdoc.Library.Entities;
using Webdoc.Library.Logic.Model;
using Webdoc.Library;
using Webdoc.Common.Extensions;
using WebApi.Security;

namespace WebApi.Support
{
    internal static class GeneralSupport
    {
        private const string pattersSeparator = "-";
        internal static readonly Regex emailNumRegex = new Regex($"^{RegexSupport.emailRegex._Trim()}{pattersSeparator}{RegexSupport.positiveIntegerRegex._Trim()}$", RegexOptions.IgnoreCase);
        internal static readonly Regex guidNumRegex = new Regex($"^{RegexSupport.guidRegex._Trim()}{pattersSeparator}{RegexSupport.positiveIntegerRegex._Trim()}$", RegexOptions.IgnoreCase);

        internal static Tuple<string, JwtSecurityToken> GetTokenData(string token)
        {
            try
            {
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                JwtSecurityToken jst = tokenHandler.ReadJwtToken(token);
                Claim claim = jst.Claims.FirstOrDefault(c => c.Type == Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Email);
                if (claim?.Value != null)
                    return new Tuple<string, JwtSecurityToken>(claim.Value, jst);
                return null;
            }
            catch
            {
                return null;
            }
        }

        internal static UserResponse GetUser(DisposableLazy<Context> context, string token, out bool expired, out bool relog, IEnumerable<int> userStatus = null, IEnumerable<int> userRoles = null)
        {
            if (_IsValidToken(token, out Tuple<string, JwtSecurityToken> tk, out expired, out SecTokenType type))
            {
                UserResponse user = _GetUser(context, SplitTokenData(tk.Item1).Item1, userStatus, userRoles);
                if (user != null)
                {
                    if (!(relog = user.Role.Id != GetUserRoleId(tk.Item2)))
                        return user;
                    return null;
                }
            }
            relog = false;
            return null;
        }

        private static UserResponse _GetUser(DisposableLazy<Context> context, string email, IEnumerable<int> userStatus = null, IEnumerable<int> userRoles = null)
        {
            return new NrUserLogic(context).GetUser(email, userStatus, userRoles);
        }

        internal static string RefreshTokenKey()
        {
            byte[] bytearray = new byte[64];
            using (RandomNumberGenerator mg = RandomNumberGenerator.Create())
            {
                mg.GetBytes(bytearray);
                return Convert.ToBase64String(bytearray);
            }
        }

        internal static bool IsValidToken(DisposableLazy<Context> context, string token, out Tuple<string, JwtSecurityToken> tk, out bool expired, out bool relog, out SecTokenType type, bool skipExpiration = false)
        {
            if (_IsValidToken(token, out tk, out expired, out type, skipExpiration))
            {
                if (type == SecTokenType.Public)
                {
                    relog = false;
                    return true;
                }
                return ExistsUser(context, tk, out relog);
            }
            return relog = false;
        }

        private static bool _IsValidToken(string token, out Tuple<string, JwtSecurityToken> tk, out bool expired, out SecTokenType type, bool skipExpiration = false)
        {
            expired = false;
            tk = null;
            type = SecTokenType.None;
            if (!string.IsNullOrWhiteSpace(token) && (tk = GetTokenData(token)) != null && !string.IsNullOrWhiteSpace(tk.Item1))
            {
                if (guidNumRegex.IsMatch(tk.Item1))
                    type = SecTokenType.Public;
                expired = tk.Item2.ValidTo <= DateTime.UtcNow;
            }
            return (skipExpiration || !expired);
        }

        private static bool ExistsUser(DisposableLazy<Context> context, Tuple<string, JwtSecurityToken> tk, out bool relog)
        {
            Tuple<int, int> user = new NrUserLogic(context).GetUserIdRoleId(SplitTokenData(tk.Item1).Item1);
            if (user != null)
                return !(relog = user.Item2 != GetUserRoleId(tk.Item2));
            return relog = false;
        }

        internal static int? GetUserRoleId(JwtSecurityToken token)
        {
            Claim claim = token.Claims.FirstOrDefault(c => c.Type == "role");
            if (claim?.Value != null && int.TryParse(claim.Value, out int roleId))
                return roleId;
            return null;
        }

        internal static Tuple<string, string> SplitTokenData(string tokenData) => new Tuple<string, string>(tokenData.Substring(0, tokenData.LastIndexOf("-")), tokenData.Substring(tokenData.LastIndexOf("-") + 1));

        internal static string JoinTokenData(string data, string key) => $"{data}{pattersSeparator}{key}";
    }
}
