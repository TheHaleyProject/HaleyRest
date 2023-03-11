using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2Test.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class GAuthController : ControllerBase {
        public GAuthController() {
        }

        //only one GET call. so no need to specify the route.
        [Route("authorised")]
        [HttpGet]
        public async Task<IActionResult> Authorised([FromQuery]string? code, [FromQuery]string? error) {
            try {
                if (string.IsNullOrEmpty(code)) {
                    return new UnauthorizedObjectResult(error ?? "Not authorised.");
                }

                GlobalHelper.SendMessage("gAuthorised", code);
                return new OkObjectResult("Success.. This window will close shortly..");

            } catch (Exception) {
                return new NotFoundObjectResult("CDEA1000: Internal Error. Please contact admin or try later.");
            }
        }

        [Route("accessToken")]
        [HttpGet]
        public async void AccessToken([FromBody] object? response) {
            try {
                

                 

            } catch (Exception) {
                
            }
        }
    }
}
