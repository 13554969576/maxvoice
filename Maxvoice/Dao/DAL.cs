using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using Maxvoice.Models;
using log4net;
using System.Data.SqlClient;

namespace Maxvoice.Dao
{
    public class MaxvoiceDAL:DbContext
    {
        public DbSet<WeChatUser> WeChatUsers { get; set; }
        public DbSet<Chat> Chat { get; set; }
        public DbSet<TSR> TSR { get; set; }

        public DbSet<EnumData> EnumData { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WeChatUser>().ToTable("wechat_user");
            modelBuilder.Entity<Chat>().ToTable("wechat_chat");
            modelBuilder.Entity<TSR>().ToTable("wechat_tsr");
            modelBuilder.Entity<EnumData>().ToTable("enum_data");
            base.OnModelCreating(modelBuilder);
        }
    }

    public class WeChatUserDao
    {
        public WeChatUser create(WeChatUser user)
        {
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();
            user =maxvoiceDAL.WeChatUsers.Add(user);
            int ret = maxvoiceDAL.SaveChanges();
            return user;
        }

        public bool update(WeChatUser user)
        {
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();
            user = maxvoiceDAL.WeChatUsers.Attach(user);
            maxvoiceDAL.Entry(user).State = EntityState.Modified;                  
            int ret = maxvoiceDAL.SaveChanges();
            return ret>0;
        }

        public bool updateOpenId(long id, string openId)
        {
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();
            WeChatUser u = maxvoiceDAL.WeChatUsers.Where(e => e.Id == id).Single();
            u.OpenId = openId;
            return maxvoiceDAL.SaveChanges() > 0;
        }

        public bool updateStateByOpenId(string openId, string state)
        {
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();
            WeChatUser u = maxvoiceDAL.WeChatUsers.Where(e => e.OpenId == openId).Single();
            if (u == null) return false;
            u.State = state;
            return maxvoiceDAL.SaveChanges() > 0;
        }
        public List<WeChatUser> getAll()
        {
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();
            return maxvoiceDAL.WeChatUsers.ToList();
        }

        public List<WeChatUser> findByStates(List<string> states)        {
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();
            return maxvoiceDAL.WeChatUsers.Where(e=> states.Contains(e.State)).ToList();
        }

        public List<WeChatUser> find(String state, DateTime? createDt)
        {
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();
            var r = maxvoiceDAL.WeChatUsers.Where(e => true == true);            
            if (state != null)
            {
                r = r.Where(e=>e.State==state);
            }
            if (createDt != null)
            {
                r = r.Where(e => e.CreateDt == createDt);
            }
            return r.ToList();
        }

        public WeChatUser getByOpenId(string openId)
        {
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();
            var r = maxvoiceDAL.WeChatUsers.Where(e => e.OpenId == openId);
            List < WeChatUser > list = r.ToList();
            if (list.Count == 1) return list[0];
            return null;
        }

        public WeChatUser getByNameAndMobile(string firstName, string lastName, string mobile)
        {
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();
            string name1 = String.Format("{0} {1}", firstName, lastName);
            string name2 = String.Format("{0}{1}", firstName, lastName);
            var r = maxvoiceDAL.WeChatUsers.Where(e =>  ((e.FirstName == firstName && e.LastName==lastName) || e.Name == name1  || e.Name == name2)  && e.Mobile==mobile) ;
            List<WeChatUser> list = r.ToList();
            if (list.Count == 1) return list[0];
            return null;
        }

        public bool isExistedUser( String email, String mobile)
        {
            //weChatId = weChatId == null ? "" : weChatId.Trim();
            email = email == null ? "" : email.Trim();
            mobile = mobile == null ? "" : mobile.Trim();
            if ( email == "" && mobile == "") return false;
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();
            return maxvoiceDAL.WeChatUsers.Any(u=>(email != "" && u.Email==email) || (mobile != "" && u.Mobile==mobile));
        }

        public bool delete(long id)
        {
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();
            WeChatUser user = maxvoiceDAL.WeChatUsers.FirstOrDefault(u=>u.Id==id);
            maxvoiceDAL.Entry(user).State = EntityState.Deleted;
            return maxvoiceDAL.SaveChanges()>0;
        }

    }

    public class ChatDao
    {
        private ILog log = log4net.LogManager.GetLogger(typeof(ChatDao));
        public Chat create(Chat chat)
        {
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();
            chat = maxvoiceDAL.Chat.Add(chat);
            int ret = maxvoiceDAL.SaveChanges();
            return chat;
        }

        public List<Chat> getUnReadMsg(string openId, string acType,int minCount)
        {          
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();           
            var r = maxvoiceDAL.Chat.Where(e => (e.CustOpenId==openId && e.ChatType==acType && e.State=="0")).OrderBy(e=>e.CreateTm);
            List<Chat> list = r.ToList();
            if(list.Count< minCount)
            {
                var r2 = maxvoiceDAL.Chat.Where(e => (e.CustOpenId == openId && e.ChatType == acType && e.State != "0")).OrderByDescending(e => e.CreateTm).Take(minCount-list.Count).OrderBy(e => e.CreateTm);
                List<Chat> list2 = r2.ToList();
                list = list2.Concat(list).ToList();
            }
            return list;
        }

        public bool markChat(long id, string state)
        {
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();
            Chat c= maxvoiceDAL.Chat.Where(e => e.Id == id).Single();
            c.State = state;
            return maxvoiceDAL.SaveChanges()>0;
        }
        public List<UnReadMsgStatistics> getUnReadMsgCount(string accType,params string[] openIds)
        {
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();
            if (openIds == null || openIds.Length == 0) return new List<UnReadMsgStatistics>();
            string tmp = "";
            int i = 1;
            object[] paras = new object[openIds.Length];
            foreach(string s in openIds)
            {
                if (i > 1) tmp += ",";
                tmp += "@para" + i;
                paras[i - 1] = new SqlParameter("para" + i, s);
                i++;
            }
            string sql = "select cust_open_id as OpenId, count(1) as cnt from wechat_chat where chat_type = '" + accType + "' and direction = '1' and state = '0' and cust_open_id in (" + tmp + ") group by cust_open_id ";
            //return maxvoiceDAL.Database.SqlQuery<UnReadMsgStatistics>("select cnt,id,name,gender,wechat_id as wechatId,mobile,email,url,state,create_dt as createDt,last_Upd_Dt as UpdateDt,first_Name as firstName,last_Name as lastName,a.openid,source,account_Type as accountType from (select cust_open_id, count(1) as cnt from wechat_chat where chat_type = '" + accType + "' and direction = '1' and state = '0' group by cust_open_id ) b left join wechat_user a on b.cust_open_id = a.openid where a.id is not null").ToList();
            //log.Debug(sql);
            //log.Debug((object[])openIds);
            return maxvoiceDAL.Database.SqlQuery<UnReadMsgStatistics>( sql,paras).ToList();
        }

        public List<string> getPendingCusts(string accType)
        {
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();
            string sql = "select distinct cust_open_id as OpenId from wechat_chat where chat_type = '" + accType + "' and direction = '1' and state = '0' ";
            return maxvoiceDAL.Database.SqlQuery<string>(sql).ToList();
        }

    }

    public class TSRDao
    {
        public TSR load(string userId) {
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();
            var r = maxvoiceDAL.TSR.Where(e => e.UserId == userId);
            List<TSR> list = r.ToList();
            if (list.Count == 1) return list[0];
            return null;
        }
    }

    public class EnumDataDao
    {
        public EnumData create(EnumData vo)
        {
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();
            vo = maxvoiceDAL.EnumData.Add(vo);
            int ret = maxvoiceDAL.SaveChanges();
            return vo;
        }

        public bool delete(long id)
        {
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();
            EnumData vo = maxvoiceDAL.EnumData.FirstOrDefault(u => u.Id == id);
            maxvoiceDAL.Entry(vo).State = EntityState.Deleted;
            return maxvoiceDAL.SaveChanges() > 0;
        }

        public List<EnumData> findEnumDataByType(string type)
        {
            MaxvoiceDAL maxvoiceDAL = new MaxvoiceDAL();
            var r = maxvoiceDAL.EnumData.Where(e =>e.Type==type );
            List<EnumData> list = r.ToList();
            return list;
        }

    }

}