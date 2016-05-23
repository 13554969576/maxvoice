using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Web.Mvc;
using Maxvoice.Models;
using System.IO;
using Maxvoice.Service;
using System.Linq;
using log4net;
using Maxvoice.Utils;
using System.Xml;
using System.Web;

namespace Maxvoice.Controllers
{

    public class WeChatController : Controller
    {
        private ILog log = log4net.LogManager.GetLogger(typeof(WeChatController));
       

        private WeChatUserService service = new WeChatUserService();
        private ChatService chatService = new ChatService();
        private TSRService tsrService = new TSRService();
        private EnumDataService enumdataService = new EnumDataService();

        public ActionResult Login(string userId, string password)
        {
            string msg;
            TSR tsr = tsrService.login(userId, password,out msg);
            if (tsr == null)
            {
                ViewData["ErrorMsg"] = msg;
                return View("Login", "BlankLayout");
            } else if (tsr.Roles=="2")
            {
                Session["user"] = tsr.UserId;
                chatService.tsrStartWorking(tsr.UserId);
                return RedirectToAction("EnterpriseAcChat", new { userId = tsr.UserId });
            } else if (tsr.Roles == "1")
            {
                Session["user"] = tsr.UserId;
                return RedirectToAction("ServiceAcChat", new { userId = tsr.UserId });
            }
            else if (tsr.Roles == "0")
            {
                ViewData["userId"] = tsr.UserId;
                Session["user"] = tsr.UserId;
                return RedirectToAction("AdminMain",new { userId= tsr.UserId });
            } else
            {
                ViewData["ErrorMsg"] = "user role is unsupported";
                return View("Login", "BlankLayout");
            }
        }

        public ActionResult logout()
        {
            Session.Abandon();
            return Redirect("/");
        }

        public ActionResult AdminMain(string userId)
        {
            log.Debug("administrator login");
            ViewData["userId"] = userId;
            return View("Admin", "BlankLayout");
        }

        public ActionResult ServiceAcChat(string userId)
        {
            ViewData["userId"] = userId;
            return View("ServiceAcChat", "BlankLayout");
        }

        public ActionResult EnterpriseAcChat(string userId)
        {
            if (Session["user"] == null)
                return Redirect("/"); 
            ViewData["userId"] = userId;
            string msg;
            List<EnumData> predefinedmsg = enumdataService.findEnumDataByType("PreDefinedMsg",out msg);
            ViewData["PreDefinedMsg"] = predefinedmsg;
            return View("EnterpriseAcChat", "BlankLayout");
        }

        public ActionResult Index()
        {
            //log.Debug( "The Import Index Page");
            return View();
        }
        public ActionResult BindServiceAccount(string code)
        {     
            log.Debug(String.Format("code is {0}", code));           
            if (code == null || code == "")
            {
                ViewData["msg"] = "You rejected the authrization request";
                return View("DoBindServiceAccountFail", "BlankLayout");
            }
            string openId = ServiceAccountUtil.GetOpenIdByOauth2Code(code);
            if (openId == null)
            {
                ViewData["msg"] = "Retrieve user infomation fail";
                return View("DoBindServiceAccountFail", "BlankLayout");
            }
            WeChatUserViewModel user = service.getByOpenId(openId);
            if(user != null)
            {
                ViewData["user"] = user;
                return View("BindServiceAccountInfo", "BlankLayout");
            }
            ViewData["openId"] = openId;
            log.Debug(String.Format("Request to bind service account, the openId is {0}", openId));
            return View("BindServiceAccount", "BlankLayout");
        }

        public ActionResult DoBindServiceAccount(string firstName, string lastName, string mobile, string openId)
        {
            WeChatUser user = service.getByOpenId(openId);
            if (user != null)
            {
                ViewData["msg"] = "You have already binded your account";
                return View("DoBindServiceAccountFail", "BlankLayout");
            }

            user = service.getByNameAndMobile(firstName, lastName, mobile);
            if (user == null)
            {
                user = new WeChatUser(){
                    FirstName= firstName,
                    LastName= lastName,
                    Mobile = mobile,
                    State = "12",
                    Source="2",
                    CreateDt = DateTime.Now,
                    AccountType="1",
                    OpenId= openId
                };
                string msg;
                service.create(user,out msg);
            } else
            {
                user.OpenId = openId;
                user.State = "12";
                string msg;
                service.update(user,out msg);
            }
            return View("DoBindServiceAccount", "BlankLayout");
        }

        public ActionResult EnterpriseAcBroadcase()
        { 
            return View("EnterpriseAcBroadcase", "BlankLayout");
        }

        public ActionResult DoEnterpriseAcBroadcase(string msgContent,string[] userlist)
        {
            string tsr = (string)Session["user"];
            string toUser = null;
            if(userlist.Length==1 && userlist[0] == "all")
            {
                toUser = "@all";
            } else
            {
                foreach(string s in userlist)
                {
                    toUser += (toUser == null? s :"|" + s);
                }
            }
            Resp resp = new Resp();
            string msg;
            bool b = EnterpriceAccountUtil.postMessage(new { touser = toUser, msgtype = "text", text = new { content = msgContent }, agentid = 0 }, out msg);
            if (b)
            {
                //msg = "Broadcast Message Successfully!";
                foreach (string s in userlist)
                {
                    string cust = s;
                    if (cust == "all") cust = "@all";
                    Chat chat = new Chat()
                    {
                        CustOpenId = cust,
                        MsgType = "text",
                        CreateTm = DateTime.Now,
                        Content = msgContent,
                        State = "10",
                        ChatType = "2",
                        Direction = "2",
                        UserId = toUser
                    };
                    chatService.create(chat);
                }
                resp.Code = 0;
                resp.Msg = "success";
            } else
            {
                resp.Code = -1;
                resp.Msg = msg;
                log.Error(msg);
            }
            return Json(resp);
        }
        public ActionResult serviceAcBroadcase()
        {
            return View("ServiceAcBroadcase", "BlankLayout");
        }

        public ActionResult DoServiceAcBroadcase(string msgContent)
        {
            string msg;
            bool b = ServiceAccountUtil.postBroadcaseMessage(new { filters = new { is_to_all = false }, msgtype = "text", text = new { content = msgContent } }, out msg);
            if (b)
            {
                msg = "Broadcast Message Successfully!";
                Chat chat = new Chat()
                {
                    CustOpenId = "@all",                   
                    MsgType = "text",
                    CreateTm = DateTime.Now,
                    Content = msgContent,
                    State = "10",
                    ChatType = "1",
                    Direction = "2",
                    UserId = "administrator"
                };
                chatService.create(chat);
            }
            ViewData["msg"] = msg;
            return View("DoBroadcase", "BlankLayout");
        }

        public JsonResult getServiceAcCustList()
        {
            List<WeChatUserViewModel> list = service.find("11",null);
            list.AddRange(service.find("12", null));
            return Json(list);
        }

        public JsonResult getEnterpriseAcCustList()
        {
            //log.Debug("getEnterpriseAcCustList");
            List<WeChatUserViewModel> list = service.find("3", null);
            //log.Debug(list.Count);
            return Json(list);
        }

        public JsonResult getServiceAcLastMsg(string openId)
        {
            List<ChatViewModel> list = chatService.getLastMsg(openId,"1");
            return Json(list);
        }

        public JsonResult getEnterpriseAcLastMsg(string openId)
        {
            List<ChatViewModel> list = chatService.getLastMsg(openId, "2");
            return Json(list);
        }

        public JsonResult getServiceAcUnReadMsg(string openId)
        {               
            List<ChatViewModel> list = chatService.getUnReadMsg(openId, "1");           
            return Json(list);
        }

        public JsonResult getEnterpriseAcUnReadMsg(string openId)
        {
            List<ChatViewModel> list = chatService.getUnReadMsg(openId, "2");
            return Json(list);
        }

        public JsonResult getServiceAcUnReadMsgCount()
        {
            string tsr = (string)Session["user"];
            List<UnReadMsgStatistics> list = chatService.getUnReadMsgCount(tsr,"1");
            return Json(list);
        }

        public JsonResult getEnterpriseAcUnReadMsgCount()
        {
            string tsr = (string)Session["user"];
            List<UnReadMsgStatistics> list = chatService.getUnReadMsgCount(tsr,"2");
            //log.Debug("getEnterpriseAcUnReadMsgCount " + tsr);
            //log.Debug(list);
            return Json(list);
        }

        public JsonResult postServiceAcMsg(string openId, string msgContent,string userId,string WPAc)
        {           
            Resp resp = new Resp();
            string msg;
            bool b = ServiceAccountUtil.postMessage(new { touser = openId, msgtype = "text", text = new { content = msgContent } }, out msg);
            if (!b)
            {
                resp.Code = -1;
                resp.Msg = msg;
                log.Error(msg);
            } else
            {
                resp.Code = 0;
                Chat chat = new Chat()
                {
                    CustOpenId = openId,
                    WPAc = WPAc,
                    MsgType = "text",
                    CreateTm = DateTime.Now,                   
                    Content = msgContent,
                    State = "10",
                    ChatType = "1",
                    Direction = "2",
                    UserId=userId
                };
                chatService.create(chat);
            }
            return Json(resp);
        }

        public JsonResult postEnterpriseAcMsg(string openId, string msgContent, string userId, string WPAc)
        {
            Resp resp = new Resp();
            string msg;
            bool b = EnterpriceAccountUtil.postMessage(new { touser = openId, msgtype = "text", text = new { content = msgContent }, agentid=0}, out msg);
            if (!b)
            {
                resp.Code = -1;
                resp.Msg = msg;
                log.Error(msg);
            }
            else
            {
                resp.Code = 0;
                Chat chat = new Chat()
                {
                    CustOpenId = openId,
                    WPAc = WPAc,
                    MsgType = "text",
                    CreateTm = DateTime.Now,
                    Content = msgContent,
                    State = "10",
                    ChatType = "2",
                    Direction = "2",
                    UserId = userId
                };
                chatService.create(chat);
                chatService.dispatchCust(openId);
            }
            return Json(resp);
        }

        public JsonResult markRead(string chatIds)
        {
            log.Debug("mark read, id list is " + chatIds);
            string[] chatIdArr = chatIds.Split(',');           
            for(int i = 0; i < chatIdArr.Length; i++)
            {
                chatService.markRead(long.Parse(chatIdArr[i])); 
            }
            Resp resp = new Resp() { Code=0,Msg="success"};
            return Json(resp);
        }
        
        [HttpGet]
        public String OnEvent(String signature,String timestamp, String nonce,String echostr)
        {
            log.DebugFormat("OnEvent(\"{0}\",\"{1}\",\"{2}\",\"{3}\")", signature, timestamp, nonce, echostr);
            return echostr;
        }

        [HttpPost]
        public String OnEvent()
        {
            //log.Debug("OnEvent");
            String postStr = HttpHelper.PostInput(Request);
            //log.Debug(postStr);
            Response.Output.Write("");
            Response.Output.Flush();
            Response.Output.Close();

            string msg;
            WeChatEvent e = ServiceAccountUtil.parseEvent(postStr,out msg);
            if (e.MsgType == "text")
            {
                handleTextMsg(e);
            } else if(e.MsgType == "event")
            {                
                if (e.Event == "CLICK" && e.Key == "M_PRIVATE_SERVICE")
                {  
                    handlePrivateServiceClick(e);
                } else if (e.Event == "unsubscribe")
                { 
                    handleUnsubscribeServiceAc(e);
                } else if (e.Event == "subscribe")
                {   
                    handleSubscribeServiceAc(e);
                }
            }         
            return "";
        }

        [HttpGet]
        public String OnEnterpriseEvent(String msg_signature, String timestamp, String nonce, String echostr)
        {
            log.DebugFormat("OnEvent(\"{0}\",\"{1}\",\"{2}\",\"{3}\")", msg_signature, timestamp, nonce, echostr);
            string sourceStr;
            EnterpriceAccountUtil.verifyCallBackInterface(msg_signature, timestamp, nonce, echostr, out sourceStr);
            return sourceStr;
        }

        [HttpPost]
        public String OnEnterpriseEvent(string msg_signature, string timestamp, string nonce)
        {
            //log.Debug("OnEnterpriseEvent");
            String postStr = HttpHelper.PostInput(Request);            
            string msg;
            WeChatEvent e = EnterpriceAccountUtil.parseEvent(msg_signature,timestamp,nonce,postStr,out msg);
            //log.Debug(string.Format("msg type: {0}, enven {1} ",e.MsgType,e.Event));
            if(e.MsgType=="text")
            {
                string s = EnterpriceAccountUtil.msgToResponceTextEvent(timestamp, nonce,e);
                Response.Output.Write(s);
                Response.Output.Flush();
                Response.Output.Close();
                handleEnterpriseTextMsg(e);
                return null;
            }
            else if(e.MsgType== "event" && e.Event== "subscribe")
            {
                service.updateStateByOpenId(e.FromUserName,"3");
            }
            else if (e.MsgType == "event" && e.Event == "unsubscribe")
            {
                service.updateStateByOpenId(e.FromUserName, "9");
            }
            Response.Output.Write("");
            Response.Output.Flush();
            Response.Output.Close();
            return null;
        }

        private void handleEnterpriseTextMsg(WeChatEvent e)
        {
            Chat chat = new Chat()
            {
                CustOpenId = e.FromUserName,
                WPAc = e.ToUserName,
                MsgType = e.MsgType,
                CreateTm = DateTime.Now,
                MsgId = e.MsgId,
                Content = e.Content,
                State = "0",
                ChatType = "2",
                Direction = "1"
            };
            chatService.create(chat);
            chatService.dispatchCust(chat.CustOpenId);
        }


        private void handleSubscribeServiceAc(WeChatEvent e)
        {
            log.Debug("User subscribe, openId is " + e.FromUserName);
            service.updateStateByOpenId(e.FromUserName, "12");            
        }

        private void handleUnsubscribeServiceAc(WeChatEvent e)
        {
            log.Debug("User unsubscribe, openId is " + e.FromUserName);
            service.updateStateByOpenId(e.FromUserName,"19");            
        }

        private void handleTextMsg(WeChatEvent e)
        {
            /*
            string openId = e.FromUserName;
            WeChatUserViewModel user = service.getByOpenId(openId);            
            if (user == null)
            {
                string c = "您还没有绑定帐号，暂不能享受私人客服的尊贵服务，非常抱歉！";
                string msg;
                bool b = ServiceAccountUtil.postMessage(new { touser = openId, msgtype = "text", text = new { content = c } }, out msg);
                if (!b)
                {
                    log.Error(String.Format("Send message fail, the reason is \"{0}\"", msg));
                }
            }
            */
            Chat chat = new Chat() { CustOpenId=e.FromUserName,WPAc=e.ToUserName,MsgType=e.MsgType,
            CreateTm=DateTime.Now,MsgId=e.MsgId,
                Content =e.Content,State="0" ,ChatType="1",Direction="1"};
            chatService.create(chat);
        }

        private void handlePrivateServiceClick(WeChatEvent weEvent)
        {
            string openId = weEvent.FromUserName;
            WeChatUserViewModel user = service.getByOpenId(openId);
            string c = null;
            if (user == null)
            {
                c = "您还没有绑定帐号，暂不能享受私人客服的尊贵服务，非常抱歉！";
            } else
            {
                c = String.Format("尊敬的{0}, 您好，请问有什么可以帮到您？",user.CustName);
            }
            string msg;
            bool b = ServiceAccountUtil.postMessage(new { touser = openId, msgtype = "text", text = new { content = c } }, out msg);
            if (!b)
            {
                log.Error(String.Format("Send message fail, the reason is \"{0}\"", msg));
            }
        }

        public ActionResult createMenu()
        {
            string msg; 
            bool b= ServiceAccountUtil.createMenu(new WeChatMenu[] { new WeChatMenu { Name="产品服务",SubButton=new WeChatMenu[] {
                new WeChatMenu() { Type="view",Url="http://www.toncentsoft.com/products/",Name="信息咨询" },
                new WeChatMenu() { Type="view",Url="http://www.toncentsoft.com/products/",Name="解决方案" },
                new WeChatMenu() { Type="view",Url="http://www.toncentsoft.com/products/",Name="定制开发" }
            } },
            new WeChatMenu() {Name="服务中心",SubButton=new WeChatMenu[] {
                new WeChatMenu() {Type="view",Name="常见问题",Url="http://www.toncentsoft.com/about-us/" },
                new WeChatMenu() {Type="click",Name="智能机器人",Key="M_ROBOT" },
                new WeChatMenu() {Type="click",Name="在线客服",Key="M_CUSTOMER_SERIVCE" }}},          
            new WeChatMenu() { Name="会员中心",SubButton=new WeChatMenu[] {
                new WeChatMenu() {Type="view",Name="帐号绑定",Url=ServiceAccountUtil.ToOauth2Url("http://wechat.toncentsoft.com/WeChat/bindserviceaccount")},
                new WeChatMenu() {Type="click",Name="我的订单",Key="M_ORDER" },
                new WeChatMenu() {Type="click",Name="私人客服",Key="M_PRIVATE_SERVICE" }}}
            }, out msg);
            if (b) msg = "菜单创建成功";
            ViewData["msg"] = msg;
            return View();
        }

        public ActionResult Invite()
        {
            ViewData["users"] = new WeChatUserService().find("0", null);
            return View("Invite", "BlankLayout");
        }

        public ActionResult DoInvite(List<WeChatUserViewModel> users)
        {
            var list = users.Where(e => e.Selected == "on");
            List<WeChatUserViewModel> ret = new List<WeChatUserViewModel>();
            foreach (WeChatUserViewModel e in list)
            {
                ret.Add(e);
                new WeChatUserService().inviteUser(e);
            }
            ViewData["users"] = ret;
            return View("DoInvite", "BlankLayout");
        }


        public ActionResult listEnterpriseMembers()
        {
            List<WeChatUserViewModel> list = service.findEnterpriseMembers();            
            ViewData["users"] = list;
            ViewData["accountType"] = "2";
            return View("List", "BlankLayout");
        }

        public ActionResult listServiceMembers()
        {
            List<WeChatUserViewModel> list = service.findServiceAcMembers();
            ViewData["users"] = list;
            ViewData["accountType"] = "1";
            return View("List", "BlankLayout");
        }

        public JsonResult deleteUser(string accountType, long id)
        {
            string msg;
            bool b= service.deleteUser(id,out msg);
            return Json(new Resp() { Code = b ? 0 : -1,Msg=msg});
        }

        public ActionResult Import()
        {
            return View("Import", "BlankLayout");
        }

        public ActionResult synchronizeEnterpriseAc()
        {
            string msg;
            List<WeChatUserViewModel> list = EnterpriceAccountUtil.getMemeberList(out msg);
            ViewData["users"] = list;            
            return View("synchronizeEnterpriseAc", "BlankLayout");
        }

        public ActionResult DoSynchronizeQyAc(List<WeChatUserViewModel> users)
        {
            foreach(WeChatUserViewModel user in users)
            {
                WeChatUserViewModel u = service.getByOpenId(user.OpenId);
                if (u != null)
                {
                    u.Name = user.Name;
                    u.Mobile = user.Mobile;
                    u.Email = user.Email;
                    u.Gender = user.Gender;
                    u.WeChatId = user.WeChatId;
                    u.State = user.State;
                    u.AccountType = "2";
                    string msg;
                    if (service.update(u.ToWeChatUser(),out msg))
                    {
                        user.ImportResult = "Update Successfully";
                    } else
                    {
                        user.ImportResult = "Update Fail";
                        user.ImportFailarReason = msg;
                    }
                        ;
                } else
                {
                    string msg;
                    WeChatUser newUser = user.ToWeChatUser();
                    newUser.AccountType = "2";
                    newUser.Source = "3";
                    newUser.CreateDt = DateTime.Now;                   
                    if (service.create(newUser, out msg) == null)
                    {
                        user.ImportResult = "Create Fail";
                        user.ImportFailarReason= msg;
                    } else
                    {
                        user.ImportResult = "Create Successfully";
                    }
                       
                }
            }
            ViewData["users"] = users;
            return View("DoSynchronizeQyAc", "BlankLayout"); ;
        }


        [HttpPost]
        public ActionResult DoImport(object obj)
        {
            string error = string.Empty;
            List<WeChatUserViewModel> users = new List<WeChatUserViewModel>();           
            ViewData["ErrorMsg"] = "";
            DataTable contactTable;
            try
            {
                foreach (string upload in Request.Files)
                {
                    if (upload != null && upload.Trim() != "")
                    {
                        string path = AppDomain.CurrentDomain.BaseDirectory + "TempData\\";
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                        System.Web.HttpPostedFileBase postedFile = Request.Files[upload];
                        string filename = Path.GetFileName(postedFile.FileName);
                        if (filename.Length > 4)
                        {
                            string strExName = filename.Substring(filename.Length - 4, 4);
                            if (strExName.ToLower() != ".xls")
                            {
                                error = "The file type is incorrect, please upload an excel file!";
                                ViewData["ErrorMsg"] = error;
                                //return View();  
                            }
                            else
                            {
                                //string filePath = Path.Combine(path, filename);  
                                string fileNamePath = path + DateTime.Now.Ticks.ToString() + ".xls";
                                postedFile.SaveAs(fileNamePath);
                                string fileExtension;
                                fileExtension = System.IO.Path.GetExtension(filename);
                                string FileType = postedFile.ContentType.ToString();  
                              
                                if (postedFile.ContentLength / 1024 <= 5120)
                                { 

                                    contactTable = ReadExcelByOledb(fileNamePath);
                                    int i = contactTable.Rows.Count;                                  
                                    string msg = string.Empty;  
                                                                   
                                    if (contactTable.Rows.Count > 1000)
                                    {
                                        error = "The row count is exceed 1000 rows";
                                        ViewData["ErrorMsg"] = error;
                                    }
                                    else
                                    {
                                        if (contactTable.Columns.Count < 5)
                                        {
                                            error = "The excel format is incorrect";
                                            ViewData["ErrorMsg"] = error;
                                        }
                                        bool isHeader = true;
                                        List<WeChatUser> usersInDb = new List<WeChatUser>();
                                        DateTime now = DateTime.Now;
                                        foreach (DataRow row in contactTable.Rows)
                                        {
                                            if (isHeader)
                                            {
                                                isHeader = false;
                                                continue;
                                            }
                                            bool b = false;
                                            String e = null;
                                            String name = row[0].ToString().Trim();
                                            String mobile = row[1].ToString().Trim();
                                            String email = row[2].ToString().Trim().ToLower();
                                            //String weChatId = row[3].ToString().Trim();
                                            String gender = row[3].ToString().Trim();
                                            String url = row[4].ToString().Trim();
                                            if (mobile !="" && !RegexUtil.isPhoneNumber(mobile))
                                            {
                                                b = true;
                                                e = "The mobile number is invalid";
                                            } else if (email!= "" && !RegexUtil.isEmail(email))
                                            {
                                                b = true;
                                                e = "The email is invalid";
                                                log.Info(String.Format("The email {0} is invalid", email));
                                            } else if (url != "" && !RegexUtil.isUrl(url))
                                            {
                                                b = true;
                                                e = "The url is invalid";
                                            } else if (mobile=="" && email=="")
                                            {
                                                b = true;
                                                e = "The mobile or the email should be provided";
                                            } else if (service.isExistedUser(email, mobile))
                                            {
                                                b = true;
                                                e = "The user already exist";
                                            }

                                            WeChatUserViewModel user = new WeChatUserViewModel() {Name= name, Mobile= mobile, Email= email,
                                                GenderText= gender,
                                                Url= url,State="0",CreateDt=now,ImportResult=b?"fail":"success",ImportFailarReason=e};
                                            if (!b)
                                            {
                                                string errmsg;
                                                WeChatUser u = service.create(user.ToWeChatUser(), out errmsg);
                                                if (u == null)
                                                {
                                                    user.ImportResult = "fail";
                                                    user.ImportFailarReason = String.Format("Insert into DB fail, the message is {0}", errmsg);
                                                } else
                                                {
                                                    user.ImportResult = "success";
                                                    user.Id = u.Id;
                                                }
                                            }
                                            usersInDb.Add(user.ToWeChatUser());
                                            users.Add(user);
                                        }
                                        //service.create(usersInDb);
                                    }

                                }
                                else
                                {
                                    error = "The file is exceed 5M, please select a smaller one!";
                                    ViewData["ErrorMsg"] = error;
                                    return View("DoImport", "BlankLayout");
                                }
                            }
                        }
                        else
                        {
                            error = "请选择需要导入的文件！";
                            ViewData["ErrorMsg"] = error;
                            return View("DoImport", "BlankLayout");  
                        }
                    }
                }
            }
            catch (Exception ex)
            {                
                ViewData["ErrorMsg"] = ex.Message;
            }
            //return Json(returnUserInfo);
            ViewData["users"] = users;
            return View("DoImport", "BlankLayout");
        }

        public ActionResult listPreDefineMsg()
        {
            string msg;
            List<EnumData> list = enumdataService.findEnumDataByType("PreDefinedMsg",out msg);
            ViewData["data"] = list;
            return View("ListPreDefineMsg", "BlankLayout");
        }

        public JsonResult deletePreMsg(long id)
        {
            string msg;
            bool b = enumdataService.deleteEnumData(id, out msg);
            return Json(new Resp() { Code = b ? 0 : -1, Msg = msg });
        }
        
        public ActionResult ImportPreDefineMsg()
        {
            return View("ImportPreDefineMsg", "BlankLayout");
        }

        [HttpPost]
        public ActionResult DoImportPreDefineMsg(object obj)
        {
            string error = string.Empty;
            List<EnumDataViewModel> data = new List<EnumDataViewModel>();
            ViewData["ErrorMsg"] = "";
            DataTable contactTable;
            try
            {
                foreach (string upload in Request.Files)
                {
                    if (upload != null && upload.Trim() != "")
                    {
                        string path = AppDomain.CurrentDomain.BaseDirectory + "TempData\\";
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                        System.Web.HttpPostedFileBase postedFile = Request.Files[upload];
                        string filename = Path.GetFileName(postedFile.FileName);
                        if (filename.Length > 4)
                        {
                            string strExName = filename.Substring(filename.Length - 4, 4);
                            if (strExName.ToLower() != ".xls")
                            {
                                error = "The file type is incorrect, please upload an excel file!";
                                ViewData["ErrorMsg"] = error;
                                //return View();  
                            }
                            else
                            {
                                //string filePath = Path.Combine(path, filename);  
                                string fileNamePath = path + DateTime.Now.Ticks.ToString() + ".xls";
                                postedFile.SaveAs(fileNamePath);
                                string fileExtension;
                                fileExtension = System.IO.Path.GetExtension(filename);
                                string FileType = postedFile.ContentType.ToString();

                                if (postedFile.ContentLength / 1024 <= 5120)
                                {

                                    contactTable = ReadExcelByOledb(fileNamePath);
                                    int i = contactTable.Rows.Count;
                                    string msg = string.Empty;

                                    if (contactTable.Rows.Count > 1000)
                                    {
                                        error = "The row count is exceed 1000 rows";
                                        ViewData["ErrorMsg"] = error;
                                    }
                                    else
                                    {
                                        if (contactTable.Columns.Count < 2)
                                        {
                                            error = "The excel format is incorrect";
                                            ViewData["ErrorMsg"] = error;
                                        }
                                        bool isHeader = true;
                                        DateTime now = DateTime.Now;
                                        foreach (DataRow row in contactTable.Rows)
                                        {
                                            if (isHeader)
                                            {
                                                isHeader = false;
                                                continue;
                                            }
                                            bool b = false;
                                            String e = null;
                                            String name = row[0].ToString().Trim();
                                            String value =  row[1].ToString().Trim();
                                            //value=HttpUtility.HtmlEncode(value);
                                            if (name == "" || value == "")
                                            {
                                                b = true;
                                                e = "The name or content of predefined message should not be empty";
                                            }

                                            EnumDataViewModel vo = new EnumDataViewModel()
                                            {
                                                Name = name,
                                                Value = value,
                                                Type="PreDefinedMsg",
                                                CreateTm = now,
                                                ImportResult = b ? "fail" : "success",
                                                ImportFailarReason = e
                                            };
                                            if (!b)
                                            {
                                                string errmsg;
                                                EnumData d = enumdataService.create(vo.ToEnumData(), out errmsg);
                                                if (d == null)
                                                {
                                                    vo.ImportResult = "fail";
                                                    vo.ImportFailarReason = String.Format("Insert into DB fail, {0}", errmsg);
                                                }
                                                else
                                                {
                                                    vo.ImportResult = "success";
                                                    vo.Id = d.Id;
                                                }
                                            }

                                            data.Add(vo);
                                        }
                                        //service.create(usersInDb);
                                    }

                                }
                                else
                                {
                                    error = "The file is exceed 5M, please select a smaller one!";
                                    ViewData["ErrorMsg"] = error;
                                    return View("DoImport", "BlankLayout");
                                }
                            }
                        }
                        else
                        {
                            error = "请选择需要导入的文件！";
                            ViewData["ErrorMsg"] = error;
                            return View("DoImport", "BlankLayout");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMsg"] = ex.Message;
            }
            //return Json(returnUserInfo);
            ViewData["data"] = data;
            return View("DoImportPreDefineMsg", "BlankLayout");
        }

        private DataTable ReadExcelByOledb(string fileNamePath)
        {
            string connStr = "Provider=Microsoft.Jet.OLEDB.4.0;Extended Properties='Excel 8.0;HDR=NO;IMEX=1';data source=" + fileNamePath;

            OleDbConnection oledbconn1 = new OleDbConnection(connStr);
            oledbconn1.Open();
            DataTable _table = oledbconn1.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });

            string strTableName = string.Empty;
            if (_table.Rows.Count > 0)
            {
                strTableName = _table.Rows[0]["TABLE_NAME"].ToString().Trim();
                string sql = string.Format("SELECT * FROM [{0}]", strTableName);
                _table = new DataTable();
                OleDbDataAdapter da = new OleDbDataAdapter(sql, oledbconn1);
                da.Fill(_table);
            }
            oledbconn1.Close();
            return _table;
        }
    }

    public class Resp
    {
        public int Code { get; set; }
        public string Msg { get; set; }
        public Object Data { get; set; }
    }

}