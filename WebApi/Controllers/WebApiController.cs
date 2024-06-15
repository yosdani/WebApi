using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Webdoc.Common.Support.LanguageSupport;
using Webdoc.Common.Exceptions;
using Webdoc.Common.Settings;
using Webdoc.Common.Support;
using Webdoc.Library.Dto;
using WebApi.Support;
using Serilog;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebApiController : ControllerBase
    {
        private static readonly LanguageObject message_invalidToken = new LanguageObject("Invalid token", "Token inválido");
        private static readonly LanguageObject message_relogToken = new LanguageObject("Login required", "Login necessário");
        private static readonly LanguageObject message_expiredToken = new LanguageObject("Expired token", "O token expirou");
        private static readonly LanguageObject message_invalidOperation = new LanguageObject("Invalid operation", "Operação inválida");
        protected internal readonly AppSettings settings = WebRestApi.Settings;
        protected internal readonly DBKeysSettings dbKeys = WebRestApi.DBKeys;
        protected internal static readonly int[] adminEditorRoles = [WebRestApi.DBKeys.UserRoles.Admin], adminEditorPartnerRoles = new List<int>(adminEditorRoles) { WebRestApi.DBKeys.UserRoles.Admin }.ToArray();

        protected internal string WebRootURL => $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        protected internal string DataURL => $"{WebRootURL}/{WebRestApi.dataPathReplacement}";

        protected internal Task<string> GetToken() => HttpContext.GetTokenAsync("access_token");

        protected internal ActionResult Default(bool expired, bool relog, bool? isEn = null)
        {
            if (expired)
                return Expired(isEn);
            if (relog)
                return Relog(isEn);
            return InvalidToken(isEn);
        }

        protected internal ActionResult Success(object? result = null, int level = 1)
        {
            Log.Logger.Information($"{string.Concat(Enumerable.Repeat(TextSupport.levelSeparator, level))}Success.");
            if (result != null)
                return Ok(new GenericResponse<object>()
                {
                    Status = ReturnStatus.Success,
                    Result = result
                });
            else
                return Ok(new BasicResponse()
                {
                    Status = ReturnStatus.Success.ToString()
                });
        }

        protected internal ActionResult InvalidOperation(LanguageObject message = null, bool? isEn = null, int level = 1)
        {
            LanguageObject msg = message ?? message_invalidOperation;
            Log.Logger.Information($"{string.Concat(Enumerable.Repeat(TextSupport.levelSeparator, level))}Invalid.");
            return BadRequest(new BasicResponse()
            {
                Status = ReturnStatus.Error.ToString(),
                Message = isEn == null ? msg : isEn.Value ? msg.En : msg.Pt
            });
        }

        protected internal ActionResult Expired(bool? isEn = null, int level = 1)
        {
            Log.Logger.Information($"{string.Concat(Enumerable.Repeat(TextSupport.levelSeparator, level))}Expired.");
            return Unauthorized(new BasicResponse()
            {
                Status = ReturnStatus.Expired.ToString(),
                Message = isEn == null ? message_expiredToken : isEn.Value ? message_expiredToken.En : message_expiredToken.Pt
            });
        }

        protected internal ActionResult InvalidToken(bool? isEn = null, int level = 1)
        {
            Log.Logger.Information($"{string.Concat(Enumerable.Repeat(TextSupport.levelSeparator, level))}Invalid.");
            return StatusCode(403, new BasicResponse()
            {
                Status = ReturnStatus.Invalid.ToString(),
                Message = isEn == null ? message_invalidToken : isEn.Value ? message_invalidToken.En : message_invalidToken.Pt
            });
        }

        protected internal ActionResult Relog(bool? isEn = null, int level = 1)
        {
            Log.Logger.Information($"{string.Concat(Enumerable.Repeat(TextSupport.levelSeparator, level))}Relog.");
            return StatusCode(403, new BasicResponse()
            {
                Status = ReturnStatus.Relog.ToString(),
                Message = isEn == null ? message_relogToken : isEn.Value ? message_relogToken.En : message_relogToken.Pt
            });
        }

        protected internal ActionResult Exception(Exception exc, bool? isEn = null)
        {
            LanguageObject msg = exc is MultilingualException mle ? mle.Message : new LanguageObject(exc.Message);
            Log.Logger.Error(exc.Message, exc);
            return StatusCode(500, new BasicResponse()
            {
                Status = ReturnStatus.Error.ToString(),
                Message = isEn == null ? msg : msg.En
            });
        }

        protected internal bool IsAdminOrEditor(int roleId) => adminEditorRoles.Any(r => r == roleId);

    }
}

