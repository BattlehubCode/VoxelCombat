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
            ContainerDiagInfo gameContainerDiagInfo = gameContainerDiag.GetContainerDiagInfo();
            GameServerDiagInfo gameServerDiagInfo = gameContainerDiag.GetDiagInfo();

            BindGameServerDiagInfo(gameContainerDiagInfo, gameServerDiagInfo);

            DDLMatchNumber.DataSource = MatchServer.Containers.Select(kvp => kvp.Key).ToArray();
            DDLMatchNumber.DataBind();

            if (!string.IsNullOrEmpty(DDLMatchNumber.SelectedValue))
            {
                MatchServerPanel.Visible = true;

                IMatchServerContainerDiagnostics matchContainerDiag = MatchServer.Containers.Where(kvp => kvp.Key == new Guid(DDLMatchNumber.SelectedValue)).Select(kvp => kvp.Value). FirstOrDefault();
                ContainerDiagInfo matchContainerDiagInfo = matchContainerDiag.GetContainerDiagInfo();
                MatchServerDiagInfo matchServerDiagInfo = matchContainerDiag.GetDiagInfo();

                BindMatchServerDiagInfo(matchContainerDiagInfo, matchServerDiagInfo);
            }
            else
            {
                MatchServerPanel.Visible = false;
            }
        }

        private void BindGameServerDiagInfo(ContainerDiagInfo diagInfo, GameServerDiagInfo gsDiagInfo)
        {
            GSConnections.Text = diagInfo.ConnectionsCount.ToString();
            GSRegisteredClients.Text = diagInfo.RegisteredClientsCount.ToString();
            GSIsMainThreadRunning.Text = diagInfo.IsMainThreadRunning.ToString();
            GSISecondaryThreadRunning.Text = diagInfo.IsSecondaryThreadRunning.ToString();
            GSActiveReplays.Text = gsDiagInfo.ActiveReplaysCount.ToString();
            GSClientsJoinedToRooms.Text = gsDiagInfo.ClientsJoinedToRoomsCount.ToString();
            GSCreatedRooms.Text = gsDiagInfo.CreatedRoomsCount.ToString();
            GSClientsWithPlayers.Text = gsDiagInfo.ClinetsWithPlayersCount.ToString();
            GSLoggedInPlayers.Text = gsDiagInfo.LoggedInPlayersCount.ToString();
            GSLoggedInBots.Text = gsDiagInfo.LoggedInBotsCount.ToString();
            GSRunningMatches.Text = gsDiagInfo.RunningMatchesCount.ToString();
        }

        private void BindMatchServerDiagInfo(ContainerDiagInfo diagInfo, MatchServerDiagInfo msDiagInfo)
        {
            MSConnections.Text = diagInfo.ConnectionsCount.ToString();
            MSContainerRegisteredClients.Text = diagInfo.RegisteredClientsCount.ToString();
            MSIsMainThreadRunning.Text = diagInfo.IsMainThreadRunning.ToString();
            MSIsSecondaryThreadRunning.Text = diagInfo.IsSecondaryThreadRunning.ToString();
            MSIsGCThreadRunning.Text = MatchServer.IsGCThreadRunningDiag.ToString();
            MSIncomingMessagesFrequency.Text = diagInfo.IncomingMessagesFrequency.ToString();
            MSOutgoingMessagesFrequency.Text = diagInfo.OutgoingMessagesFrequency.ToString();
            MSIsInitializationStarted.Text = msDiagInfo.IsInitializationStarted.ToString();
            MSIsInitialized.Text = msDiagInfo.IsInitialized.ToString();
            MSIsEnabled.Text = msDiagInfo.IsEnabled.ToString();
            MSIsMatchEngineCreated.Text = msDiagInfo.IsMatchEngineCreated.ToString();
            MSIsReplay.Text = msDiagInfo.IsReplay.ToString();
            MSServerRegisteredClients.Text = msDiagInfo.ServerRegisteredClientsCount.ToString();
            MSReadyToPlayCLients.Text = msDiagInfo.ReadyToPlayClientsCount.ToString();
            MSClientsWithPlayers.Text = msDiagInfo.ClientsWithPlayersCount.ToString();
            MSPlayers.Text = msDiagInfo.PlayersCount.ToString();
            MSBots.Text = msDiagInfo.BotsCount.ToString();
        }

        protected void Timer1_Tick(object sender, EventArgs e)
        {
        }
    }
}