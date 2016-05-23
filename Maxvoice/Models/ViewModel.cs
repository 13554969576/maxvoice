using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maxvoice.Models
{
    [NotMapped]
    public class WeChatUserViewModel : WeChatUser
    {
        public String GenderText
        {
            get
            {
                String gender = Gender;
                if (gender == null) return null;
                if (gender != "2" && gender != "1") throw new FormatException("The Gender should only be 1 or 2");
                return Gender == "2" ? "Female" : "Male";
            }

            set
            {
                if (value == null || value.Trim() == "")
                {
                    Gender = null;
                    return;
                }
                else if (value.ToLower() == "f" || value.ToLower() == "female")
                {
                    Gender = "2";
                    return;
                }
                else if (value.ToLower() == "m" || value.ToLower() == "male")
                {
                    Gender = "1";
                    return;
                }
                else
                {
                    throw new ArgumentException("the gender should be f or female or m or male");
                }

            }
        }

        public String StateText
        {
            get
            {
                String state = State;
                if (state == "0")
                {
                    return "New"; //新建
                }
                else if (state == "1")
                {
                    return "Enterprise Account Created"; //已新建企业号帐户
                }
                else if (state == "2")
                {
                    return "Sent Invite";//已发出企业号邀请
                }
                else if (state == "3")
                {
                    return "Accepted Invite";//已关注企业号
                }
                else if (state == "9")
                {
                    return "Enterprise Account Closed";//已取消企业号关注
                }
                else if (state == "11")
                {
                    return "Service Account Created"; //已关注服务号
                }
                else if (state == "12")
                {
                    return "Binded Account";//已绑定服务号帐号
                }
                else if (state == "19")
                {
                    return "Service Account Closed";//已取消服务号关注
                }
                else if (state == "99")
                {
                    return "Deleted";//已删除用户
                }
                throw new FormatException("Invalid state found");
            }
        }

        public string AccountTypeText
        {
            get
            {
                String acType = AccountType;
                if (acType == "0")
                {
                    return "None Account"; //新建
                }
                else if (acType == "1")
                {
                    return "Service Account"; 
                }
                else if (acType == "2")
                {
                    return "Enterprise Account";
                }
                return null;
            }
        }

        public string SourceText
        {
            get
            {
                String source = Source;
                if (source == "1")
                {
                    return "Imported";
                }
                else if (source == "2")
                {
                    return "Bind Account";
                }
                else if (source == "3")
                {
                    return "Sync From EnterpriseAc";
                }
                return null;
            }
        }

        public string CustName
        {
            get
            {
                if (Name != null) return Name;
                return String.Format("{0}{1}", FirstName, LastName);
            }
        }

        public String ImportResult { get; set; }
        public String ImportFailarReason { get; set; }
        public String Selected { get; set; }

        public WeChatUser ToWeChatUser()
        {
            return new WeChatUser() { Email = this.Email, Gender = this.Gender, Id = this.Id, Mobile = this.Mobile, Name = this.Name, State = this.State, Url = this.Url, WeChatId = this.WeChatId, CreateDt = this.CreateDt,AccountType=this.AccountType, OpenId=this.OpenId, Source=this.Source, FirstName=this.FirstName, LastName=this.LastName };
        }

        public WeChatUserViewModel() { }

        public WeChatUserViewModel(WeChatUser user)
        {
            Email = user.Email;
            Gender = user.Gender;
            Id = user.Id;
            Mobile = user.Mobile;
            Name = user.Name;
            State = user.State;
            Url = user.Url;
            WeChatId = user.WeChatId;
            CreateDt = user.CreateDt;
            UpdateDt = user.UpdateDt;
            FirstName = user.FirstName;
            LastName = user.LastName;
            AccountType = user.AccountType;
            OpenId = user.OpenId;
            Source = user.Source;
        }
    }

    [NotMapped]
    public class ChatViewModel : Chat
    {
        public ChatViewModel() { }
        public ChatViewModel(Chat chat)
        {
            Id = chat.Id;
            CustOpenId = chat.CustOpenId;
            WPAc = chat.WPAc;
            MsgType = chat.MsgType;
            MsgId = chat.MsgId;
            CreateTm = chat.CreateTm;
            Content = chat.Content;
            Url = chat.Url;
            MediaId = chat.MediaId;
            MediaFormat = chat.MediaFormat;
            ThumbMediaId = chat.ThumbMediaId;
            State = chat.State;            
            UserId = chat.UserId;
            Direction = chat.Direction;          
        }
    }

    [NotMapped]
    public class EnumDataViewModel : EnumData
    {
        public String ImportResult { get; set; }
        public String ImportFailarReason { get; set; }

        public EnumData ToEnumData()
        {
            return new EnumData() {Id=this.Id, Type=this.Type, Name=this.Name,Value=this.Value, CreateTm=this.CreateTm, Remark=this.Remark};
        }
    }
}

    