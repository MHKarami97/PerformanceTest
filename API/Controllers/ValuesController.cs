using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;

namespace API.Controllers
{
    public class ValuesController : ApiController
    {
        private const int SleepTimeInMillisecond = 30;

        // GET api/values
        public string Get()
        {
            Thread.Sleep(SleepTimeInMillisecond);
            
            return "value";
        }

        // POST api/values
        public void Post()
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}