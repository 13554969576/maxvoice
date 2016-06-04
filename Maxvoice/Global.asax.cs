using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using log4net;
using Maxvoice.Service;

namespace Maxvoice
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private ILog log = log4net.LogManager.GetLogger(typeof(MvcApplication));
        ChatService chatService = new ChatService();
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            string s = System.Configuration.ConfigurationManager.AppSettings["EncodingToken"];
            chatService.initWorking();
        }

        public void Session_End()
        {
            string tsr = (string)Session["user"];
            chatService.tsrOffDuty(tsr);
            log.Debug("session end. " + tsr);
        }

        public void Session_Start() {
            //log.Debug("session start " + Session["user"]);
        }
    }
}
