using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Maxvoice.Models
{
    public class WeChatUser
    {
        [System.ComponentModel.DataAnnotations.Key]
        public long Id { get; set; }

        public String Name { get; set; }

        [Column("first_name")]
        public String FirstName { get; set; }

        [Column("last_name")]
        public String LastName { get; set; }

        public String Mobile { get; set; }

        public String Email { get; set; }

        [Column("wechat_id")]
        public String WeChatId { get; set; }

        public String Gender { get; set; }

        public String Url { get; set; }

        public String State { get; set; }

        [Column("create_dt")]
        public DateTime CreateDt { get; set; }

        [Column("last_upd_dt")]
        [Timestamp]
        public byte[] UpdateDt { get; set; }

        public String OpenId { get; set; }

        [Column("account_type")]
        public String AccountType { get; set; }

        public String Source { get; set; }
    }

    public class Chat
    {
        [System.ComponentModel.DataAnnotations.Key]
        public long Id { get; set; }

        [Column("cust_open_id")]
        public String CustOpenId { get; set; }

        [Column("wp_ac")]
        public String WPAc { get; set; }

        [Column("msg_type")]
        public String MsgType { get; set; }

        [Column("msg_id")]
        public String MsgId { get; set; }

        [Column("create_tm")]
        public DateTime CreateTm { get; set; }

        [Column("msg_content")]
        public string Content { get; set; }

        public string Url { get; set; }

        [Column("media_id")]
        public string MediaId { get; set; }

        [Column("media_format")]
        public string MediaFormat { get; set; }

        [Column("thumb_media_id")]
        public string ThumbMediaId { get; set; }

        public string State { get; set; }       

        [Column("user_id")]
        public string UserId { get; set; }

        [Column("chat_type")]
        public string ChatType { get; set; }

        public string Direction { get; set; }
    }

    public class TSR
    {
        [System.ComponentModel.DataAnnotations.Key]
        [Column("user_id")]
        public string UserId { get; set; }

        public string Name { get; set; }

        public string Sex { get; set; }

        public string UnionId { get; set; }

        [Column("role")]
        public string Roles { get; set; }

        public string Password { get; set; }

        [Column("create_date")]
        public DateTime CreateDt { get; set; }        
    }
   
    public class UnReadMsgStatistics
    {  
        public int Cnt { get; set; }
        public String OpenId { get; set; }
    }


    public class WeChatEvent
    {
        public string ToUserName { get; set; }
        public string FromUserName { get; set; }
        public DateTime CreateTime { get; set; }
        public string MsgType { get; set; }
        public string MsgId { get; set; }
        public string Content { get; set; }
        public string Location_X { get; set; }
        public string Location_Y { get; set; }
        public string Scale { get; set; }
        public string Label { get; set; }
        public string PicUrl { get; set; }
        public string Url { get; set; }
        public string Key { get; set; }
        public string Event { get; set; }
        public string AgentID { get; set; }
    }

    public class EnumData
    {
        [System.ComponentModel.DataAnnotations.Key]
        public long Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string Remark { get; set; }
        [Column("create_tm")]
        public DateTime CreateTm { get; set; }
    }
}