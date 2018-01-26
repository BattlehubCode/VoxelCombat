using System;
using System.Web;
using log4net;

namespace Battlehub.VoxelCombat
{
    public class Global : HttpApplication
    {
        private GameServerContainer m_container;

        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            //AreaRegistration.RegisterAllAreas();
            //GlobalConfiguration.Configure(WebApiConfig.Register);
            //RouteConfig.RegisterRoutes(RouteTable.Routes);        

            log4net.Config.XmlConfigurator.Configure();

            m_container = GameServerContainer.Instance;
            m_container.Run();

        }

        void Application_End()
        {
            m_container.Stop();
        }

        void Application_Error(object sender, EventArgs e)
        {
          
            Exception exc = Server.GetLastError();

            ILog log = LogManager.GetLogger(typeof(Global));
            log.Error(exc.Message, exc);

            // Clear the error from the server
            Server.ClearError();
        }

    }
}