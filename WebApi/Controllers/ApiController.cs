using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Webdoc.Common.Support.LanguageSupport;
using WebApi.Auth;
using Webdoc.Common.Exceptions;
using Webdoc.Common.Support;
using Webdoc.Library.Dto;
using Webdoc.Library.Models;
using Serilog;
using Log = Serilog.Log;

namespace WebApi.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class ApiController(IJwtAuthenticationService authService) : Controller
    {
        private readonly IJwtAuthenticationService _authService = authService;

        [HttpGet("get_publictoken")]
        public IActionResult GetPublicToken()
        {
            try
            {
                Serilog.Log.Logger.Information($"GetPublicToken.");
                return Success(new
                {
                    Token = _authService.GetToken_GUID(out DateTime? expire),
                    ExpiresUtc = expire
                });
            }
            catch (Exception exc)
            {
                return Exception(exc);
            }
        }
        [AllowAnonymous]
        [HttpGet]
        public object Get()
        {
            var responseObject = new
            {
                Status = "Running",

            };
            Log.Logger.Information($"APIStatus: {responseObject.Status}");
            return responseObject;
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

    }
}

