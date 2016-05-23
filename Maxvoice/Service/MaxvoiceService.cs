using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Maxvoice.Dao;
using Maxvoice.Models;
using Maxvoice.Utils;
using Newtonsoft.Json;
using log4net;
using System.Reflection;
using System.Data;

namespace Maxvoice.Service
{
    public class WeChatUserService
    {
        private WeChatUserDao dao = new WeChatUserDao();
        private ILog log = log4net.LogManager.GetLogger(typeof(WeChatUserService));

        public void create(List<WeChatUser> users)
        {
            if (users == null || users.Count == 0) return;
            foreach (WeChatUser user in users)
            {
                string msg;
                if (create(user,out msg) == null)
                {
                    throw new Exception(msg);
                };
            }
        }

        public WeChatUser create(WeChatUser user,out string msg)
        {
            msg = null;
            try
            {
                return dao.create(user);
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                msg = e.Message;
                return null;
            }
        }

        public bool update(WeChatUser user,out string msg)
        {
            msg = null;
            try
            {
                bool b= dao.update(user);
                if (!b)
                {
                    msg = "no record be updated";
                }
                return b;
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateConcurrencyException e)
            {
                log.Error(e.Message, e);
                msg = "no change detected";
                return false;
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                msg=e.Message;
                return false;
            }
        }

        public bool updateOpenId(long id, string openId)
        {
            try
            {
                return dao.updateOpenId(id,openId);
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                throw e;
            }
        }

        public bool updateStateByOpenId(string openId, string state)
        {
            try
            {
                return dao.updateStateByOpenId(openId, state);
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                throw e;
            }
        }

        public bool isExistedUser(String email, String mobile)
        {
            try
            {
                return dao.isExistedUser(email, mobile);
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                throw e;
            }

        }

        public List<WeChatUserViewModel> getAll()
        {
            try
            {
                return weChatUserToViewModel(dao.getAll());
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                throw e;
            }
        }

        public List<WeChatUserViewModel> findEnterpriseMembers()
        {
            try
            {
                return weChatUserToViewModel(dao.findByStates(new string[] { "0","1","2","3","9"}.ToList()));
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                throw e;
            }
        }

        public List<WeChatUserViewModel> findServiceAcMembers()
        {
            try
            {
                return weChatUserToViewModel(dao.findByStates(new string[] { "11", "12", "19"}.ToList()));
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                throw e;
            }
        }

        public List<WeChatUserViewModel> find(String state, DateTime? createDt)
        {
            try
            {
                return weChatUserToViewModel(dao.find(state, createDt));
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                throw e;
            }

        }

        public WeChatUserViewModel getByOpenId(String openId)
        {
            try
            {
                WeChatUser user = dao.getByOpenId(openId);
                if (user == null) return null;
                return new WeChatUserViewModel(user);
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                throw e;
            }
        }


        public WeChatUser getByNameAndMobile(string firstName, string lastName, string mobile)
        {
            try
            {
                WeChatUser user = dao.getByNameAndMobile(firstName, lastName, mobile);
                if (user == null) return null;
                return new WeChatUserViewModel(user);
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                throw e;
            }
        }

        public bool inviteUser(WeChatUserViewModel user)
        {
            string msg;
            bool b= EnterpriceAccountUtil.createMember(user,out msg);
            if (!b)
            {
                user.ImportResult = "fail";
                user.ImportFailarReason = msg;
                return false;
            }   
                     
            dao.updateOpenId(user.Id, user.OpenId);
            dao.updateStateByOpenId(user.OpenId,"1");
            /*
            b = EnterpriceAccountUtil.inviteMember(user.OpenId,out msg);           
            if (!b)
            {
                user.ImportResult = "fail";
                user.ImportFailarReason = msg;
                return false;
            };
            dao.updateStateByOpenId(user.OpenId, "2");
            */
            user.ImportResult="success";
            return true;
        }

        private List<WeChatUserViewModel> weChatUserToViewModel(List<WeChatUser> users)
        {
            if (users == null) return new List<WeChatUserViewModel>();
            List<WeChatUserViewModel> list = new List<WeChatUserViewModel>();
            foreach (WeChatUser user in users)
            {
                list.Add(new WeChatUserViewModel(user));
            }
            return list;
        }

        public bool deleteUser(long id, out string msg)
        {
            msg = null;
            try
            {
                bool b= dao.delete(id);
                if (!b)
                {
                    msg = "none record deleted";
                }
                return b;
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                msg = e.Message;
                return false;
            }
        }



        public class WeChatResult
        {
            public int errcode { get; set; }
            public String errmsg { get; set; }
        }
    }
    public class ChatService
    {
        private ChatDao dao = new ChatDao();
        private ILog log = log4net.LogManager.GetLogger(typeof(ChatService));

        private static IDictionary<string, List<string>> tSRCustMapping = new Dictionary<string, List<string>>();
        private static IDictionary<string, string> custTSRMapping = new Dictionary<string, string>();
        private static List<string> pendingCusts;

        public void dispatchCusts(List<string> custs)
        {
            foreach (string cust in custs)
            {
                dispatchCust(cust);
            }
        }

        public string dispatchCust(string cust)
        {
            if (custTSRMapping.ContainsKey(cust))
            {
                return custTSRMapping[cust];
            }

            int m = Int32.MaxValue;
            string ret = null;
            foreach (string tsr in tSRCustMapping.Keys)
            {
                if (tSRCustMapping[tsr].Count < m)
                {
                    m = tSRCustMapping[tsr].Count;
                    ret = tsr;
                }
            }
            log.Debug("dispatcher the customer " + cust +" to " + ret);
            tSRCustMapping[ret].Add(cust);
            custTSRMapping.Add(cust, ret);
            return ret;
        }

        public bool dispatchCust(string cust,string tsr)
        {
            if (custTSRMapping.ContainsKey(cust))
            {
                return false;
            }
            log.Debug("dispatcher the customer " + cust + " to " + tsr + "as required");
            tSRCustMapping[tsr].Add(cust);
            custTSRMapping.Add(cust, tsr);
            return true;
        }

        public void initWorking()
        {
            List<string> custs = getPendingCusts("2");
            if (custs == null || custs.Count == 0) return;
            pendingCusts = custs;
        }

        public void tsrStartWorking(string tsr)
        {
            if (tSRCustMapping.ContainsKey(tsr)) return;
            tSRCustMapping.Add(tsr, new List<string>());
            if (pendingCusts != null)
            {
                List<string> custs = pendingCusts;
                pendingCusts = null;
                dispatchCusts(custs);
            }
        }

        public void tsrOffDuty(string tsr)
        {
            List<string> custs = tSRCustMapping[tsr];
            tSRCustMapping.Remove(tsr);
            string[] custArr = custs.ToArray();
            foreach(string cust in custArr)
            {
                custTSRMapping.Remove(cust);
            }
            List<UnReadMsgStatistics> list= getUnReadMsgCountByOpenIds("2", custArr);
            if (list==null || list.Count==0)return ;
            custs = new List<string>();
            foreach (UnReadMsgStatistics us in list)
            {
                custs.Add(us.OpenId);
            }

            if (tSRCustMapping.Count > 0)
            {
                dispatchCusts(custs);
            } else
            {
                pendingCusts = custs;
            }
        }

        public Chat create(Chat chat)
        {
            try
            {
                return dao.create(chat);
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                throw e;
            }
        }

        public List<ChatViewModel> getLastMsg(String openId, string acType)
        {
            try
            {
                List<Chat> list = dao.getUnReadMsg(openId, acType, 20);
                return chatToViewModel(list);
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                throw e;
            }
        }

        public List<ChatViewModel> getUnReadMsg(String openId, string acType)
        {
            try
            {
                List<Chat> list = dao.getUnReadMsg(openId, acType, -1);
                return chatToViewModel(list);
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                throw e;
            }
        }

        public void markRead(long id)
        {
            try
            {
               dao.markChat(id, "1");
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                throw e;
            }
        }

        private List<ChatViewModel> chatToViewModel(List<Chat> chats)
        {
            if (chats == null) return new List<ChatViewModel>();
            List<ChatViewModel> list = new List<ChatViewModel>();
            foreach (Chat chat in chats)
            {
                list.Add(new ChatViewModel(chat));
            }
            return list;
        }

        public List<UnReadMsgStatistics> getUnReadMsgCount(string tsr,string acType)
        {
            return getUnReadMsgCountByOpenIds(acType,tSRCustMapping[tsr].ToArray());
        }

        public List<string> getPendingCusts(string acType)
        {
            try
            {
                return dao.getPendingCusts(acType);
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                throw e;
            }
        }

        public List<UnReadMsgStatistics> getUnReadMsgCountByOpenIds(string acType,params string[] openIds)
        { 
            try
            {
                return dao.getUnReadMsgCount(acType, openIds);
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                throw e;
            }
        }

    }

    public class TSRService
    {
        private TSRDao dao = new TSRDao();
        private ILog log = log4net.LogManager.GetLogger(typeof(TSRService));

        public TSR load(string userId)
        {
            try
            {
                return dao.load(userId);
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                throw e;
            }
        }

        public TSR login(string userId, string password, out string msg)
        {
            msg = null;
            try
            {
                TSR tsr= dao.load(userId);
                if (tsr==null)
                {
                    msg = "User does not exist";
                    return null;
                } else if (tsr.Password != password)
                {
                    msg = "User name or password is incorrect";
                    return null;
                }
                return tsr;
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                msg = e.Message;
                return null;
            }

        }
    }

    public class EnumDataService
    {
        private EnumDataDao dao = new EnumDataDao();
        private ILog log = log4net.LogManager.GetLogger(typeof(EnumDataService));

        public EnumData create(EnumData vo, out string msg)
        {
            msg = null;
            try
            {
                return dao.create(vo);
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                msg = e.Message;
                return null;
            }
        }

        public bool deleteEnumData(long id, out string msg)
        {
            msg = null;
            try
            {
                bool b = dao.delete(id);
                if (!b)
                {
                    msg = "none record deleted";
                }
                return b;
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                msg = e.Message;
                return false;
            }
        }

        public List<EnumData> findEnumDataByType(string type, out string msg)
        {
            msg = null;
            try
            {
                return dao.findEnumDataByType(type);
            }
            catch (Exception e)
            {
                log.Error(e.Message, e);
                msg = e.Message;
                return null;
            }
        }
    }
}