using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Maxvoice
{
    public class Program
    {
        public void test()
        {
             
            var c = new { access_token="", expires_in="", errcode="", errmsg="" };
            String json = "{\"access_token\":\"DilX9oSwUmv_NzIQvAM1dbzWgorSUK6s9QC09-WVxUG5B_ngTFkzScUvEOaNYw3p\",\"expires_in\":7200}";
            TokenResult obj = JsonConvert.DeserializeObject<TokenResult>(json);
            if (obj.access_token != null) { }  ;
           
        }

        public class TokenResult
        {
            public String access_token { get; set; }
            public int expires_in { get; set; }
            public int errcode { get; set; }
            public String errmsg { get; set; }
         }
    }
}