using log4net;
using Maxvoice.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Xml;

namespace Maxvoice.Utils
{
    public class EnterpriceAccountUtil
    {
        private static ILog log = log4net.LogManager.GetLogger(typeof(EnterpriceAccountUtil));
        
        //Token, during setting the callback url, need to provide a token to verify the new callback url
        private const string EncodingToken = "tongyanxi";
        //EncodingToken, during setting the callback url, need to provide a EncodingToken to verify the new callback url
        private const string EncodingAESKey = "5RTfixO8WSIfvNTOYUs4c6fhklOPgNrzJSjtQPgrpww";
        //the url to request the AccessToken
        private const string URL_TOKEN = "https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid={0}&corpsecret={1}";  
        //the url to send service message      
        private const string URL_SEND_MESSAGE = "https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token={0}";
        //the url to request to create a new member
        private const string URL_CREATE_MEMBER = "https://qyapi.weixin.qq.com/cgi-bin/user/create?access_token={0}";
        //the url to request to invite a wechat memeber
        private const string URL_INVITE_MEMBER = "https://qyapi.weixin.qq.com/cgi-bin/invite/send?access_token={0}";
        //the url to get the wechat enterprie account member list
        private const string URL_GET_MEMBER_LIST = "https://qyapi.weixin.qq.com/cgi-bin/user/list?access_token={0}&department_id={1}&fetch_child={2}&status={3}";
        //Crop id, can be found in the wechat account information, use to request the AccessToken
        private static string CROP_ID = "wxf4774c756a351ff8";
        //Manager Group Secret, can be found in the manager group information, use to request the AccessToken
        private static string CROP_SECRET = "FrP-Z44ewUnc766wAjWDEHIHHWzT7o_eGCsLKFQFToKTTXVRqwyVrBy4pupVpgVP";
        private static String token = null;
        private static DateTime? tokenExpiredTm = null;

        public static Object tokenLock = new Object();

        //AccessToken, use to send access the wechat server, can request from wechat server using crop id and cecret, 
        //it genereated by the tencent wechat server, and will be expired in 7200 senconds or more, which is specified in the 
        //response in the ExpiresIn field.
        public static string Token
        {
            get
            {
                lock (tokenLock)
                {
                    if (token == null || tokenExpiredTm - DateTime.Now < new TimeSpan(0, 0, 0, 1))
                    {
                        String json = HttpHelper.sendHttpRequest(String.Format(URL_TOKEN, CROP_ID, CROP_SECRET), "");
                        TokenResult ret = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenResult>(json);
                        if (ret.ErrCode != 0)
                        {
                            log.Error(String.Format("Get token fail, the error message is \"{0}\"", ret.ErrMsg));
                            return null;
                        }
                        token = ret.AccessToken;
                        tokenExpiredTm = DateTime.Now + new TimeSpan(0, 0, 0, ret.ExpiresIn);
                    }
                    return token;
                }
            }
        }

        //create wechat enterprise account member, if the token request fail or member create fail, return false, 
        //and the fail message will to put into the msg arg.
        public static bool createMember(WeChatUser user,out string msg)
        {
            msg = null;
            String token = EnterpriceAccountUtil.Token;
            if (token == null)
            {
                msg = "request wechat token fail";
                return false;
            }

            String memberId = user.WeChatId;
            if (memberId == null) memberId = user.Mobile;
            if (memberId == null) memberId = user.Email;

            var data = new
            {
                userid = memberId,
                name = user.Name,
                department = new int[] { 1 },
                position = "",
                mobile = user.Mobile,
                gender = user.Gender,
                email = user.Email,
                weixinid = user.WeChatId
            };
            var sData = JsonConvert.SerializeObject(data);
            String json = HttpHelper.sendHttpRequest(String.Format(URL_CREATE_MEMBER, token), sData);
            WeChatResult ret = JsonConvert.DeserializeObject<WeChatResult>(json);
            if (ret.errcode != 0)
            {
                msg = String.Format("create user fail, {0}, the error code is {1}", ret.errmsg,ret.errcode);
                return false;
            };
            user.OpenId = memberId;
            return true;
        }

        //invite a wechat enterprise account member to subscribe the account, if the token request fail or invite request fail, 
        //return false, and the fail message will to put into the msg arg.
        public static bool inviteMember(string openId, out string msg)
        {
            msg = null;
            String token = EnterpriceAccountUtil.Token;
            if (token == null)
            {
                msg = "request wechat token fail";
                return false;
            }

            var data = new { userid = openId };
            string sData = JsonConvert.SerializeObject(data);
            string json = HttpHelper.sendHttpRequest(String.Format(URL_INVITE_MEMBER, token), sData);
            WeChatResult  ret = JsonConvert.DeserializeObject<WeChatResult>(json);
            if (ret.errcode != 0)
            { 
                msg = String.Format("invite user fail, {0}, the error code is {1}", ret.errmsg,ret.errcode);
                return false;
            };
            return true;
        }

        //post message to member, if the token request fail or post message request fail, 
        //return false, and the fail message will to put into the msg arg.
        public static bool postMessage(Object data, out string msg)
        {
            msg = null;
            String token = EnterpriceAccountUtil.Token;
            if (token == null)
            {
                msg = "request wechat token fail";
                return false;
            }
            var sData = JsonConvert.SerializeObject(data);
            log.Debug(sData);
            String json = HttpHelper.sendHttpRequest(String.Format(URL_SEND_MESSAGE, Token), sData);
            WeChatResult ret = JsonConvert.DeserializeObject<WeChatResult>(json);
            if (ret.errcode == 0) return true;
            msg = ret.errmsg;
            log.Error(String.Format("message send fail, the reason is {0}", msg));
            return false;
        }

        //during setting the wechat application callback url, the wechat server will verify the new url
        public static bool verifyCallBackInterface(string msg_signature, string timestamp,string nonce, string echostr, out string sourceStr)
        {
            Tencent.WXBizMsgCrypt wxcpt = new Tencent.WXBizMsgCrypt(EncodingToken, EncodingAESKey, CROP_ID);            
            int ret = 0;
            sourceStr = "";
            ret = wxcpt.VerifyURL(msg_signature, timestamp, nonce, echostr, ref sourceStr);
            if (ret != 0)
            {
                log.Error("ERR: VerifyURL fail, ret: " + ret);
                return false;
            }
            return true;
        }

        //parse the event sent by toncent wechat server, the event interface is defined by Tencent, for the detail, can refer to Tencent website,
        //the event content is in xml format, and the code is be encrypted.  
        public static WeChatEvent parseEvent(string msg_signature, string timestamp,string nonce,string source, out string msg)
        {
            msg = null;
            Tencent.WXBizMsgCrypt wxcpt = new Tencent.WXBizMsgCrypt(EncodingToken, EncodingAESKey, CROP_ID);
            string xml = "";
            int ret = wxcpt.DecryptMsg(msg_signature, timestamp, nonce, source, ref xml);
            if (ret != 0)
            {
                msg = "ERR: Decrypt Enterprise Event Fail, ret: " + ret;
                log.Error(msg);
                return null;
            }
            log.Debug(xml);
            WeChatEvent e = new WeChatEvent();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);           
            XmlElement rootElement = doc.DocumentElement;  
            e.ToUserName = rootElement.SelectSingleNode("ToUserName").InnerText;
            e.FromUserName = rootElement.SelectSingleNode("FromUserName").InnerText;
            string sTm = rootElement.SelectSingleNode("CreateTime").InnerText;
            e.CreateTime = DateTimeUtil.toDataTime(sTm);
            e.MsgType = rootElement.SelectSingleNode("MsgType").InnerText;            
            e.AgentID= rootElement.SelectSingleNode("AgentID").InnerText;
            switch (e.MsgType)
            {
                case "text":
                    e.MsgId = rootElement.SelectSingleNode("MsgId").InnerText;
                    e.Content = rootElement.SelectSingleNode("Content").InnerText;                    
                    break;
                case "image":
                    e.MsgId = rootElement.SelectSingleNode("MsgId").InnerText;
                    e.PicUrl = rootElement.SelectSingleNode("PicUrl").InnerText;
                    break;
                case "location":
                    e.MsgId = rootElement.SelectSingleNode("MsgId").InnerText;
                    e.Location_X = rootElement.SelectSingleNode("Location_X").InnerText;
                    e.Location_Y = rootElement.SelectSingleNode("Location_Y").InnerText;
                    e.Scale = rootElement.SelectSingleNode("Scale").InnerText;
                    e.Label = rootElement.SelectSingleNode("Label").InnerText;
                    break;
                case "link":
                    break;
                case "event":
                    e.Event = rootElement.SelectSingleNode("Event").InnerText;
                    e.Key = rootElement.SelectSingleNode("EventKey").InnerText;                    
                    break;
            }
            return e;
        }

        //after the demo application received the event, it should be responsed in 5 secends, otherwise, the wechat server will
        //send the event repeadly at most three times.
        public static string msgToResponceTextEvent(string timestamp, string nonce,WeChatEvent e)
        {
            Tencent.WXBizMsgCrypt wxcpt = new Tencent.WXBizMsgCrypt(EncodingToken, EncodingAESKey, CROP_ID);
            string sRespData = String.Format("<xml><ToUserName><![CDATA[{0}]]></ToUserName><FromUserName><![CDATA[{1}]]></FromUserName><CreateTime>{2}</CreateTime><MsgType><![CDATA[text]]></MsgType><Content><![CDATA[]]></Content><MsgId>{3}</MsgId><AgentID>{4}</AgentID></xml>",e.FromUserName,e.ToUserName, timestamp, e.MsgId,e.AgentID);
            string sEncryptMsg = ""; 
            int ret = wxcpt.EncryptMsg(sRespData, timestamp, nonce, ref sEncryptMsg);
            if (ret != 0)
            {
                log.Error("ERR: EncryptMsg Fail, ret: " + ret);
                return null;
            }
            //log.Debug("Responce message; " + sEncryptMsg);
            return sEncryptMsg;
        }

        //get member list in the wechat enterprise account address book, if the token request fail or get member list fail, 
        //return false, and the fail message will to put into the msg arg.
        public static List<WeChatUserViewModel> getMemeberList(out string msg)
        {
            msg = null;
            String token = EnterpriceAccountUtil.Token;
            if (token == null)
            {
                msg = "request wechat token fail";
                return null;
            }
            try
            {
                String json = HttpHelper.sendHttpRequest(String.Format(URL_GET_MEMBER_LIST, token, "1", "1", "0"), "");
                GetMemeberListResult ret = JsonConvert.DeserializeObject<GetMemeberListResult>(json);
                if (ret.errcode != 0)
                {
                    msg = ret.errmsg;
                    return null;
                } else
                {
                    List<WeChatUserViewModel> list = new List<WeChatUserViewModel>();
                    foreach (Member m in ret.userlist)
                    {
                        string state = m.status;
                        state = state == "1" ? "3" : (state=="2"?"9":"1");
                        list.Add(new WeChatUserViewModel() {OpenId=m.userid,Name=m.name,Mobile=m.mobile,Email=m.email,Gender=m.gender,WeChatId=m.weixinid,State=state });
                    }
                    return list;

                }
            } catch (Exception e)
            {
                msg = e.Message;
                return null;
            }
        }

        private class GetMemeberListResult
        {
            public int errcode { get; set; }
            public string errmsg { get; set; }
            public List<Member> userlist { get; set; }
        }

        private class Member
        {
            public string userid { get; set; }
            public string name { get; set; }
            public List<int> department { get; set; }
            public string position { get; set; }
            public string mobile { get; set; }
            public string gender { get; set; }
            public string email { get; set; }
            public string weixinid { get; set; }
            public string avatar { get; set; }
            public string status { get; set; }
        }
    }

    public class ServiceAccountUtil
    {
        private static ILog log = log4net.LogManager.GetLogger(typeof(ServiceAccountUtil));
        private const string URL_TOKEN = "https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={0}&secret={1}";
        private const String URL_SEND_MESSAGE = "https://api.weixin.qq.com/cgi-bin/message/custom/send?access_token={0}";
        private const String URL_MENU_CREATE = "https://api.weixin.qq.com/cgi-bin/menu/create?access_token={0}";
        private const string URL_OAUTH2 = "https://open.weixin.qq.com/connect/oauth2/authorize?appid={0}&redirect_uri={1}&response_type=code&scope=snsapi_base&state=123#wechat_redirect";
        private const string URL_OAUTH2TOKEN = "https://api.weixin.qq.com/sns/oauth2/access_token?appid={0}&secret={1}&code={2}&grant_type=authorization_code";
        private const string URL_SEND_MESSAGE_ALL = "https://api.weixin.qq.com/cgi-bin/message/mass/sendall?access_token={0}";
        private static string app_id = "wx2ff6ff3af902f2a6";
        private static string app_secret = "3c49828a60a04d9750c73f99d65dc577";

        private static string token = null;
        private static DateTime? tokenExpiredTm = null;

        private static string oauth2Token = null;
        private static DateTime? oauth2TokenExpiredTm = null;

        private static Object tokenLock = new Object();

        public static string Token
        {
            get
            {
                lock (tokenLock)
                {
                    if (token == null || tokenExpiredTm - DateTime.Now < new TimeSpan(0, 0, 0, 1))
                    {
                        String json = HttpHelper.sendHttpRequest(String.Format(URL_TOKEN, app_id, app_secret), "");
                        TokenResult ret = JsonConvert.DeserializeObject<TokenResult>(json);
                        if (ret.ErrCode != 0)
                        {
                            log.Error(String.Format("Get token fail, the error message is \"{0}\"", ret.ErrMsg));
                            return null;
                        }
                        token = ret.AccessToken;
                        tokenExpiredTm = DateTime.Now + new TimeSpan(0, 0, 0, ret.ExpiresIn);
                    }
                    return token;
                }
            }
        }

        public static bool postMessage(Object data, out string msg)
        {
            msg = null;
            String token = Token;
            if (token == null)
            {
                msg = "request wechat token fail";
                return false;
            }
            var sData = JsonConvert.SerializeObject(data);
            log.Debug(sData);
            String json = HttpHelper.sendHttpRequest(String.Format(URL_SEND_MESSAGE, token), sData);
            WeChatResult ret = JsonConvert.DeserializeObject<WeChatResult>(json);
            if (ret.errcode == 0) return true;
            msg = ret.errmsg;
            log.Error(String.Format("message send fail, the reason is {0}", msg));
            return false;
        }

        public static bool postBroadcaseMessage(Object data, out string msg)
        {
            msg = null;
            String token = Token;
            if (token == null)
            {
                msg = "request wechat token fail";
                return false;
            }
            var sData = JsonConvert.SerializeObject(data);
            log.Debug(sData);
            String json = HttpHelper.sendHttpRequest(String.Format(URL_SEND_MESSAGE_ALL, token), sData);
            WeChatResult ret = JsonConvert.DeserializeObject<WeChatResult>(json);
            if (ret.errcode == 0) return true;
            msg = ret.errmsg;
            log.Error(String.Format("message send fail, the reason is {0}", msg));
            return false;
        }

        public static bool createMenu(WeChatMenu[] menus, out string msg)
        {
            var obj = new { button = menus };
            msg = null;
            String token = Token;
            if (token == null)
            {
                msg = "request wechat token fail";
                return false;
            }
            var jSetting = new JsonSerializerSettings();
            jSetting.NullValueHandling = NullValueHandling.Ignore;
            var sData = JsonConvert.SerializeObject(obj, jSetting);
            log.Debug(sData);
            String json = HttpHelper.sendHttpRequest(String.Format(URL_MENU_CREATE, token), sData);
            WeChatResult ret = JsonConvert.DeserializeObject<WeChatResult>(json);
            if (ret.errcode == 0) return true;
            msg = ret.errmsg;
            return false;
        }

        public static string ToOauth2Url(string url)
        {
            url = System.Web.HttpUtility.UrlEncode(url, Encoding.UTF8);
            return string.Format(URL_OAUTH2, app_id, url);
        }

        public static string GetOpenIdByOauth2Code(string code)
        {
            String json = HttpHelper.sendHttpRequest(String.Format(URL_OAUTH2TOKEN, app_id, app_secret, code), "");
            TokenResult ret = JsonConvert.DeserializeObject<TokenResult>(json);
            if (ret.ErrCode != 0)
            {
                log.Error(String.Format("Get oauth2 token fail, the error code is {0}, the error message is \"{1}\"", ret.ErrCode, ret.ErrMsg));
                return null;
            }
            oauth2Token = ret.AccessToken;
            oauth2TokenExpiredTm = DateTime.Now + new TimeSpan(0, 0, 0, ret.ExpiresIn);
            return ret.OpenId;
        }

        public static WeChatEvent parseEvent(string source, out string msg)
        {
            msg = null;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(source);
            XmlElement rootElement = doc.DocumentElement;
            WeChatEvent e = new WeChatEvent();
            e.ToUserName = rootElement.SelectSingleNode("ToUserName").InnerText;
            e.FromUserName = rootElement.SelectSingleNode("FromUserName").InnerText;
            string sTm = rootElement.SelectSingleNode("CreateTime").InnerText;
            e.CreateTime = DateTimeUtil.toDataTime(sTm);
            e.MsgType = rootElement.SelectSingleNode("MsgType").InnerText;            
            //log.Debug(String.Format("The message type is {0}, the message content is {1}", e.MsgType, e.Content));           
            switch (e.MsgType)
            {
                case "text":
                    e.MsgId = rootElement.SelectSingleNode("MsgId").InnerText;
                    e.Content = rootElement.SelectSingleNode("Content").InnerText;
                    //handleTextMsg(e);
                    break;
                case "image":
                    e.MsgId = rootElement.SelectSingleNode("MsgId").InnerText;
                    e.PicUrl = rootElement.SelectSingleNode("PicUrl").InnerText;
                    break;
                case "location":
                    e.MsgId = rootElement.SelectSingleNode("MsgId").InnerText;
                    e.Location_X = rootElement.SelectSingleNode("Location_X").InnerText;
                    e.Location_Y = rootElement.SelectSingleNode("Location_Y").InnerText;
                    e.Scale = rootElement.SelectSingleNode("Scale").InnerText;
                    e.Label = rootElement.SelectSingleNode("Label").InnerText;
                    break;
                case "link":
                    break;
                case "event":
                    e.Event = rootElement.SelectSingleNode("Event").InnerText;
                    e.Key = rootElement.SelectSingleNode("EventKey").InnerText;
                    if (e.Event == "CLICK" && e.Key == "M_PRIVATE_SERVICE")
                    {
                        //handlePrivateServiceClick(e);
                    }
                    break;
            }
            return e;
        }
    }

    public class TokenResult
    {
        [JsonProperty(PropertyName = "access_token")]
        public String AccessToken{ get; set; }

        [JsonProperty(PropertyName = "expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty(PropertyName = "refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty(PropertyName = "errcode")]
        public int ErrCode { get; set; }

        [JsonProperty(PropertyName = "errmsg")]
        public String ErrMsg { get; set; }

        [JsonProperty(PropertyName = "openid")]
        public String OpenId { get; set; }

        [JsonProperty(PropertyName = "scope")]
        public String Scope { get; set; }
    }

    public class WeChatResult
    {
        public int errcode { get; set; }
        public String errmsg { get; set; }        
    }


    public class WeChatMenu
    {
        [JsonProperty(PropertyName = "button")]
        public String Button { get; set; }

        [JsonProperty(PropertyName = "sub_button")]
        public WeChatMenu[] SubButton { get; set; }

        [JsonProperty(PropertyName = "type")]
        public String Type { get; set; }

        [JsonProperty(PropertyName = "name")]
        public String Name { get; set; }

        [JsonProperty(PropertyName = "key")]
        public String Key { get; set; }

        [JsonProperty(PropertyName = "url")]
        public String Url { get; set; }

        [JsonProperty(PropertyName = "media_id")]
        public String MediaId { get; set; }
    }

}