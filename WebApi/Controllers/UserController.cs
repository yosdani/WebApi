using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Webdoc.Common.Request.UserRequests;
using static Webdoc.Common.Support.LanguageSupport;
using System.IdentityModel.Tokens.Jwt;
using WebApi.Auth;
using WebApi.Security;
using WebApi.Support;
using Webdoc.Common.Response;
using Webdoc.Common.Support;
using Webdoc.Library.Entities;
using Webdoc.Library;
using Serilog;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : WebApiController
    {
        private static readonly LanguageObject message_wrongCredentials = new LanguageObject("Wrong credentials", "Credenciais erradas");
        private static readonly LanguageObject message_userActivation = new LanguageObject("Webdoc user activation", "Ativação do utilizador do Webdoc");
        private static readonly LanguageObject message_passRecovery = new LanguageObject("Webdoc password recover attempt", "Tentativa de recuperação de senha do Webdoc");
        private static Dictionary<string, Tuple<string, byte[]>> registrationFooter = new Dictionary<string, Tuple<string, byte[]>>() { { "footer", new Tuple<string, byte[]>("image/png", WebRestApi.Resource_RegistrationFooter) } };
        private readonly JwtAuthenticationService _authService;

        public UserController(JwtAuthenticationService authService)
        {
            _authService = authService;
        }

        [HttpPost("authenticate")]
        public async Task<ActionResult> Authenticate([FromBody] UserAuthenticate request)
        {
            try
            {
                Log.Logger.Information($"Authenticate.{TextSupport.customSeparator}For: {request}.");
                using DisposableLazy<Context> context = WebRestApi.GetLazyContext(retrier: settings.DataAccess.ConnectionRetrier);
                if (GeneralSupport.IsValidToken(context, await GetToken(), out Tuple<string, JwtSecurityToken> tk, out bool expired, out bool relog, out SecTokenType type) && type == SecTokenType.Public)
                {
                    Tuple<string, UserResponse> result = _authService.Authenticate(settings, request, out DateTime? expires, out LanguageObject message);
                    if (result != null)
                        return Success(new
                        {
                            User = result.Item2,
                            Token = result.Item1,
                            ExpiresUtc = expires
                        });
                    return InvalidOperation(message ?? message_wrongCredentials);
                }
                return Default(expired, relog);
            }
            catch (Exception exc)
            {
                return Exception(exc);
            }
        }

    }
}



