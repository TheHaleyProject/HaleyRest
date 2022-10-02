using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net.Http;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RestCallTests.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CallBackController : ControllerBase
    {
        [Route("authorised")]
        [HttpGet]
        public bool Authorised(string oauth_token,string oauth_verifier) {

            return true; 
        }

        [HttpGet]
        public int GetSomething() {
            return 0;
        }

        public CallBackController() {
        }
    }
}
