using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Maxvoice.Startup))]
namespace Maxvoice
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
