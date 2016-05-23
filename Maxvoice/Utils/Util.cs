using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Maxvoice.Utils
{   
    public class HttpHelper
    {
        public static string PostInput(HttpRequestBase request)
        {
            try
            {
                return PostInput(request, Encoding.UTF8);
            }
            catch (Exception ex)
            { throw ex; }
        }

        public static string PostInput(HttpRequestBase request, Encoding encoding)
        {
            StringBuilder builder = new StringBuilder();
            try
            {
                using (System.IO.Stream s = request.InputStream)
                {
                    int count = 0;
                    byte[] buffer = new byte[1024];
                    while ((count = s.Read(buffer, 0, 1024)) > 0)
                    {
                        builder.Append(encoding.GetString(buffer, 0, count));
                    }
                    s.Flush();
                    s.Close();
                    s.Dispose();
                }
                return builder.ToString();
            }
            catch (Exception ex)
            { throw ex; }
        }

        public bool PostOutput(System.Web.HttpResponse response, string content)
        {
            try
            {
                return PostOutput(response, content, Encoding.UTF8);
            }
            catch (Exception ex)
            { throw ex; }
        }

        public bool PostOutput(System.Web.HttpResponse response, string content, Encoding encoding)
        {
            StringBuilder builder = new StringBuilder();
            try
            {
                using (System.IO.Stream s = response.OutputStream)
                {
                    byte[] bys = encoding.GetBytes(content);
                    s.Write(bys, 0, bys.Length);
                    s.Flush();
                    s.Close();
                    s.Dispose();
                }
                return true;
            }
            catch (Exception ex)
            { throw ex; }
        }

        public static String sendHttpRequest(String url, String data)
        {
           // WebClient x;
            WebClient wc = new WebClient();
            Encoding enc = Encoding.UTF8;
            return enc.GetString(wc.UploadData(url, enc.GetBytes(data)));
            /*
                        Stream outstream = null;
                        Stream instream = null;
                        StreamReader sr = null;
                        HttpWebResponse response = null;
                        HttpWebRequest request = null;
                        Encoding encoding = Encoding.UTF8;
                        byte[] postData = encoding.GetBytes(data);           
                        try
                        {
                            request = WebRequest.Create(url) as HttpWebRequest;
                            CookieContainer cookieContainer = new CookieContainer();
                            request.CookieContainer = cookieContainer;
                            request.AllowAutoRedirect = true;
                            request.Method = "POST";
                            request.ContentType = "application/x-www-form-urlencoded";
                            request.ContentLength = data.Length;
                            outstream = request.GetRequestStream();
                            outstream.Write(postData, 0, data.Length);
                            outstream.Close();               
                            response = request.GetResponse() as HttpWebResponse;               
                            instream = response.GetResponseStream();
                            sr = new StreamReader(instream, encoding);
                            string content = sr.ReadToEnd();
                            string err = string.Empty;
                            //Response.Write(content);
                            return content;
                        }catch (Exception ex)
                        {
                            string err = ex.Message;
                            return string.Empty;
                        } */

        }

    }

    public class RegexUtil
    {
        public static bool isEmail(string strIn)
        {
            return Regex.IsMatch(strIn, @"^[_a-z\d\-\./]+@[_a-z\d\-]+(\.[_a-z\d\-]+)*(\.(info|biz|com|edu|gov|net|am|bz|cn|cx|hk|jp|tw|vc|vn))$");
        }

        public static bool isPhoneNumber(string strIn)
        {
            //手机号正则表达式
            //return Regex.IsMatch(strIn, @"^(13|15|18)[0-9]{9}$");
            //固话号正则表达式
            //return Regex.IsMatch(strIn, @"^(\d{3,4}-?)?\d{7,8}$");

            return Regex.IsMatch(strIn, @"^[+]?[0-9\-)( ]{6,20}$");
        }

        public static bool isUrl(string strIn)
        {
            return Regex.IsMatch(strIn, @"^(http|https|ftp)\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&$%\$#\=~])*$");
        }
    }

    public class DateTimeUtil
    {
        public static DateTime toDataTime(string dtTmByLong)
        {
            long iTm = long.Parse(dtTmByLong) * 1000L;
            DateTime dt_1970 = new DateTime(1970, 1, 1, 0, 0, 0);
            long tricks_1970 = dt_1970.Ticks;                         
            long time_tricks = tricks_1970 + iTm * 10000;                       
            DateTime dt = new DateTime(time_tricks).AddHours(8);
            return dt;
        }
    }
}