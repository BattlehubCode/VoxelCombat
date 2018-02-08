<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="Battlehub.VoxelCombat.Dashboard" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Dashboard</title>
</head>
<body>
    <form id="form1" runat="server">

        <asp:ScriptManager runat="server"></asp:ScriptManager>
        <asp:Timer ID="Timer1" runat="server" Interval="5000" OnTick="Timer1_Tick">
        </asp:Timer>
        <asp:UpdatePanel runat="server" UpdateMode="Conditional">
            <Triggers>
                <asp:AsyncPostBackTrigger ControlID="Timer1" EventName="Tick" />
            </Triggers>
            <ContentTemplate>

                <div>
                    <h1>Voxel Combat Server</h1>
                    <div>
                        <h2>Game Server</h2>
                        <div style="float: left;margin-right:10px;">
                            <span>Connections:</span><br />
                            <span>Registered Clients:</span><br />
                            <span>Is Main Thread Running:</span><br />
                            <span>Is Secondary Thread Running:</span><br />
                            <span>Active Replays:</span><br />
                            <span>Clients Joined To Rooms:</span><br />
                            <span>Created Rooms:</span><br />
                            <span>Clients with players:</span><br />
                            <span>Logged-in Players:</span><br />
                            <span>Logged-in Bots:</span><br />
                            <span>Runnting Matches:</span><br />
                        </div>
                        <div>
                            <asp:Label runat="server" ID="GSConnections" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="GSRegisteredClients" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="GSIsMainThreadRunning" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="GSISecondaryThreadRunning" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="GSActiveReplays" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="GSClientsJoinedToRooms" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="GSCreatedRooms" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="GSClientsWithPlayers" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="GSLoggedInPlayers" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="GSLoggedInBots" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="GSRunningMatches" Text="-"></asp:Label><br />
                        </div>
                        <div style="clear:both"></div>
                        <h2>Match Server</h2>
                        <asp:DropDownList runat="server" ID="DDLMatchNumber">
                        </asp:DropDownList>
                        <br />
                        <div style="float: left;margin-right:10px;">
                            <span>Connections:</span><br />
                            <span>Container Registered Clients:</span><br />
                            <span>Is Main Thread Running:</span><br />
                            <span>Is Secondary Thread Running:</span><br />
                            <span>Incoming Messages Frequency:</span><br />
                            <span>Outgoing Messages Frequency:</span><br />
                            <span>Is Initialization Started:</span><br />
                            <span>Is Initialized:</span><br />
                            <span>Is Enabled:</span><br />
                            <span>Is Match Engine Created:</span><br />
                            <span>Is Replay:</span><br />
                            <span>Server Registered Clients:</span><br />
                            <span>Ready To Play Clients:</span><br />
                            <span>Clients with players:</span><br />
                            <span>Players:</span><br />
                            <span>Bots:</span><br />
                        </div>
                  
                        <div style="float: left">
                            <asp:Label runat="server" ID="MSConnections" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="MSContainerRegisteredClients" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="MSIsMainThreadRunning" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="MSIsSecondaryThreadRunning" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="MSIncomingMessagesFrequency" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="MSOutgoingMessagesFrequency" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="MSIsInitializationStarted" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="MSIsInitialized" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="MSIsEnabled" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="MSIsMatchEngineCreated" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="MSIsReplay" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="MSServerRegisteredClients" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="MSReadyToPlayCLients" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="MSClientsWithPlayers" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="MSPlayers" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="MSBots" Text="-"></asp:Label><br />
                        </div>
                        <div style="clear:both"></div>
                    </div>
                </div>
            </ContentTemplate>
        </asp:UpdatePanel>
    </form>
</body>
</html>
