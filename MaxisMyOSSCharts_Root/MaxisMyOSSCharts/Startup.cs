using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(MaxisMyOSSCharts.Startup))]
namespace MaxisMyOSSCharts
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
