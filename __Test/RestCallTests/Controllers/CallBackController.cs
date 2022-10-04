using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net.Http;

#if NETCOREAPP3_1_OR_GREATER || NET5_0_OR_GREATER
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
#elif NET472_OR_GREATER
using System.Web.Http;
#endif

using Haley.Models;
using Haley.Abstractions;
using Haley.Events;
using Haley.Enums;
using Haley.Utils;

namespace RestCallTests.Controllers {

    [Route("api/[controller]")]
#if NETCOREAPP3_1_OR_GREATER || NET5_0_OR_GREATER
    [ApiController]
    public class CallBackController : ControllerBase
#elif NET472_OR_GREATER
    public class CallBackController : ApiController 
#endif
    {
        [Route("authorised")]
        [HttpGet]
        public async Task<string> Authorised(string oauth_token, string oauth_verifier) {
            try {
                if (string.IsNullOrEmpty(oauth_token) || string.IsNullOrWhiteSpace(oauth_verifier)) return "Verifier and token values cannot be empty";
                Task.Run(() => { EventStore.Singleton.GetEvent<TokenAuthorisedEvent>().Publish((oauth_verifier, oauth_token)); });
                return "Please close this window and return to your application.";
            } catch (Exception ex) {
                return "Internal Error. Please contact admin or try later";
            }
        }

        [HttpGet]
        public int GetSomething() {
            return 0;
        }

        public CallBackController() {
        }
    }

}
