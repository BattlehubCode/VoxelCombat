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
                            <span>Registered clients:</span><br />
                            <span>Is main thread running:</span><br />
                            <span>Is secondary thread running:</span><br />
                            <span>Active replays:</span><br />
                            <span>Clients joined to rooms:</span><br />
                            <span>Created rooms:</span><br />
                            <span>Clients with players:</span><br />
                            <span>Logged-in players:</span><br />
                            <span>Logged-in bots:</span><br />
                            <span>Runnting matches:</span><br />
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
                        <asp:Panel runat="server" ID="MatchServerPanel">
                            <div style="float: left;margin-right:10px;">
                            <span>Connections:</span><br />
                            <span>Container registered Clients:</span><br />
                            <span>Is main thread running:</span><br />
                            <span>Is secondary thread running:</span><br />
                            <span>Is GC thread running:</span><br />
                            <span>Incoming messages frequency:</span><br />
                            <span>Outgoing messages frequency:</span><br />
                            <span>Is initialization started:</span><br />
                            <span>Is initialized:</span><br />
                            <span>Is enabled:</span><br />
                            <span>Is match engine created:</span><br />
                            <span>Is replay:</span><br />
                            <span>Server registered clients:</span><br />
                            <span>Ready to play clients:</span><br />
                            <span>Clients with players:</span><br />
                            <span>Players:</span><br />
                            <span>Bots:</span><br />
                        </div>
                  
                        <div style="float: left">
                            <asp:Label runat="server" ID="MSConnections" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="MSContainerRegisteredClients" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="MSIsMainThreadRunning" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="MSIsSecondaryThreadRunning" Text="-"></asp:Label><br />
                            <asp:Label runat="server" ID="MSIsGCThreadRunning" Text="-"></asp:Label><br />
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
                        </asp:Panel>
                        
                    </div>
                </div>
            </ContentTemplate>
        </asp:UpdatePanel>
    </form>
</body>
</html>
