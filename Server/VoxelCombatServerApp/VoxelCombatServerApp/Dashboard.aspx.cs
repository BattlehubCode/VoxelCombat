using System;
using System.Linq;
using System.Web.UI;

namespace Battlehub.VoxelCombat
{
    public partial class Dashboard : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            IGameServerContainerDiagnostics gameContainerDiag = GameServerContainer.Instance;
            IGameServerDiagnostics gameServerDiag = gameContainerDiag.GameServer;

            ContainerDiagInfo gameContainerDiagInfo = gameContainerDiag.GetDiagInfo();
            GameServerDiagInfo gameServerDiagInfo = gameServerDiag.GetDiagInfo();

            BindGameServerDiagInfo(gameContainerDiagInfo, gameServerDiagInfo);

            if(DDLMatchNumber.SelectedIndex >= 0)
            {
                IMatchServerContainerDiagnostics matchContainerDiag = MatchServer.Containers.ElementAtOrDefault(DDLMatchNumber.SelectedIndex);
                IMatchServerDiagnostics matchServerDiag = matchContainerDiag.MatchServer;

                ContainerDiagInfo matchContainerDiagInfo = matchContainerDiag.GetDiagInfo();
                MatchServerDiagInfo matchServerDiagInfo = matchServerDiag.GetDiagInfo();

                BindMatchServerDiagInfo(matchContainerDiagInfo, matchServerDiagInfo);
            }
        }

        private void BindGameServerDiagInfo(ContainerDiagInfo diagInfo, GameServerDiagInfo gsDiagInfo)
        {

        }

        private void BindMatchServerDiagInfo(ContainerDiagInfo diagInfo, MatchServerDiagInfo msDiagInfo)
        {

        }

        protected void Timer1_Tick(object sender, EventArgs e)
        {

        }
    }
}