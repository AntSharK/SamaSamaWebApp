using Microsoft.Owin;
using Owin;
using SamaSamaLAN.Controllers;

[assembly: OwinStartup(typeof(SamaSamaLAN.Startup))]

namespace SamaSamaLAN
{
    /// <summary>
    /// Startup class
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Configuration of app
        /// </summary>
        public void Configuration(IAppBuilder app)
        {
            JoinController.Configure(app);
            MasterController.Configure(app);
            VotingController.Configure(app);
            PlayController.Configure(app);
        }
    }
}
