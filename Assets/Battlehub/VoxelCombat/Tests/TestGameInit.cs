using System;
using System.Linq;
using UnityEngine;

namespace Battlehub.VoxelCombat
{

    public class TestGameInitArgs
    {
        public string MapName;
        public int PlayersCount = GameConstants.DefaultTestInitPlayersCount;
        public int BotsCount = GameConstants.DefaultTestInitBotsCount;
    }

    public static class TestGameInit
    {
        private static readonly string[] m_playerNames =
        {
            "Red",
            "Blue",
            "Green",
            "Yellow",
            "Orange",
            "Purple",
            "Teal",
            "Black"
        };

        public static void Init(string mapName, int playersCount, int botsCount, bool preferRemote, Action callback, Action<Error> error)
        {
            if(playersCount < 0)
            {
                throw new ArgumentOutOfRangeException("playersCount");
            }

            if(botsCount < 0)
            {
                throw new ArgumentOutOfRangeException("botsCount");
            }

            if(playersCount > GameConstants.MaxLocalPlayers)
            {
                throw new ArgumentException("playersCount > " + GameConstants.MaxLocalPlayers);
            }

            if (playersCount + botsCount > 8 || playersCount + botsCount < 2)
            {
                throw new ArgumentException("players + bots should be >= 2 and <= 8");
            }

            //IProgressIndicator progress = Dependencies.Progress;
            //progress.IsVisible = true;

            INotification notification = Dependencies.Notification;
            IGameServer remoteGameServer = Dependencies.RemoteGameServer;
           
            if (preferRemote)
            {
                if (remoteGameServer.IsConnectionStateChanging)
                {
                    InitGameOnConnectionStateChanged(preferRemote, mapName, playersCount, botsCount, callback, error, notification, remoteGameServer);
                }
                else if (!remoteGameServer.IsConnected)
                {
                    InitGameOnConnectionStateChanged(preferRemote, mapName, playersCount, botsCount, callback, error, notification, remoteGameServer);
                    remoteGameServer.Connect();
                }
                else
                {
                   LogoffIfNeeded(() =>
                   {
                       InitGame(mapName, 0, 0, playersCount, botsCount, callback, error);
                   });
                    
                }
            }
            else
            {
                if (remoteGameServer != null && remoteGameServer.IsConnectionStateChanging)
                {
                    InitGameOnConnectionStateChanged(preferRemote, mapName, playersCount, botsCount, callback, error, notification, remoteGameServer);
                }
                else if (remoteGameServer != null && remoteGameServer.IsConnected)
                {
                    InitGameOnConnectionStateChanged(preferRemote, mapName, playersCount, botsCount, callback, error, notification, remoteGameServer);
                    remoteGameServer.Disconnect();
                }
                else
                {
                    LogoffIfNeeded(() =>
                    {
                        InitGame(mapName, 0, 0, playersCount, botsCount, callback, error);
                    });
                }
            }
        }

        private static void InitGameOnConnectionStateChanged(bool preferRemote, string mapName, int playersCount, int botsCount, Action callback, Action<Error> error, INotification notification, IGameServer remoteGameServer)
        {
            ServerEventHandler<ValueChangedArgs<bool>> connectionStateChanged = null;
            connectionStateChanged = (Error e, ValueChangedArgs<bool> payload) =>
            {       
#warning CHECK if actually unsubscribed
               
                if (!remoteGameServer.HasError(e))
                {
                    if (preferRemote)
                    {
                        if (!remoteGameServer.IsConnected)
                        {
                            remoteGameServer.Connect();
                            return;
                        }
                    }
                    else
                    {
                        if(remoteGameServer.IsConnected)
                        {
                            remoteGameServer.Disconnect();
                            return;
                        }
                    }

                    LogoffIfNeeded(() =>
                    {
                        InitGame(mapName, 0, 0, playersCount, botsCount, callback, error);
                    });
                }
                else
                {
                    notification.ShowError(e);

                    LogoffIfNeeded(() =>
                    {
                        InitGame(mapName, 0, 0, playersCount, botsCount, callback, error);
                    });
                }

                remoteGameServer.ConnectionStateChanged -= connectionStateChanged;
            };

            remoteGameServer.ConnectionStateChanged += connectionStateChanged;
        }

        private static void CreateDefaultMap(Action<Error, MapInfo> completed)
        {
            MapRoot emptyMap = new MapRoot(8);

            IGameServer server = Dependencies.GameServer;
            IGlobalSettings gSettings = Dependencies.Settings;

            Guid mapId = Guid.NewGuid();
            MapInfo mapInfo = new MapInfo
            {
                Id = mapId,
                Name = "Default",
                MaxPlayers = GameConstants.MaxPlayers,
                SupportedModes = GameMode.All
            };

            byte[] bytes = ProtobufSerializer.Serialize(emptyMap);
            MapData mapData = new MapData
            {
                Id = mapInfo.Id,
                Bytes = bytes,
            };

            server.UploadMap(gSettings.ClientId, mapInfo, mapData, error => completed(error, mapInfo));
        }


        private static void LogoffIfNeeded(Action done)
        {
            IGameServer server = Dependencies.GameServer;
            IGlobalSettings gSettings = Dependencies.Settings;

            server.GetPlayers(gSettings.ClientId, (e0, players) =>
            {
                if (players.Length == 0)
                {
                    done();
                }
                else
                {
                    server.Logoff(gSettings.ClientId, players.Select(p => p.Id).ToArray(), (e1, guid) =>
                    {
                        done();
                    });
                }
            });

        }
        private static void InitGame(string mapName, int playerIndex, int botIndex, int playersCount, int botsCount, Action callback, Action<Error> error)
        {
            //IProgressIndicator progress = Dependencies.Progress;
            IGameServer server = Dependencies.GameServer;
            IGlobalSettings gSettings = Dependencies.Settings;


            if (playerIndex < playersCount)
            {
                server.Login(m_playerNames[playerIndex], "welcome", gSettings.ClientId, (e1, playerId, pwdHash) =>
                {
                    if (server.HasError(e1))
                    {
                        error(e1);
                            //progress.IsVisible = false;
                            server.SignUp(m_playerNames[playerIndex], "welcome", gSettings.ClientId, (e100, p, pwdHash2) =>
                        {
                            if (server.HasError(e100))
                            {
                                error(e100);
                                    //progress.IsVisible = false;
                                    return;
                            }

                            LoginOrSignupCompleted(mapName, playerIndex, botIndex, playersCount, botsCount, callback, error);
                        });
                    }
                    else
                    {
                        LoginOrSignupCompleted(mapName, playerIndex, botIndex, playersCount, botsCount, callback, error);
                    }
                });
            }
            else
            {
                if (botIndex == botsCount)
                {
                    Launch(mapName, callback, error, null, /*progress*/ server, gSettings);
                }
                else
                {
                    server.CreateBot(gSettings.ClientId, m_playerNames[playersCount + botIndex], BotType.Hard, (e4, botId, roomWithBot) =>
                    {
                        if (server.HasError(e4))
                        {
                            error(e4);
                                //progress.IsVisible = false;
                                return;
                        }

                        botIndex++;

                        if (botIndex == botsCount)
                        {
                            Launch(mapName, callback, error, null /*progress*/ , server, gSettings);
                        }
                        else
                        {
                            InitGame(mapName, playerIndex, botIndex, playersCount, botsCount, callback, error);
                        }
                    });
                }
            }
        }

        private static void Launch(string mapName, Action callback, Action<Error> error, IProgressIndicator progress, IGameServer server, IGlobalSettings gSettings)
        {
            server.SetReadyToLaunch(gSettings.ClientId, true, (readyToLaunchError, room) =>
            {
                if (server.HasError(readyToLaunchError))
                {
                    error(readyToLaunchError);
                    //progress.IsVisible = false;
                    return;
                }

                server.Launch(gSettings.ClientId, (e5, serverUrl) =>
                {
                    if (server.HasError(e5))
                    {
                        error(e5);
                        //progress.IsVisible = false;
                        return;
                    }

                    //progress.IsVisible = false;

                    gSettings.MatchServerUrl = serverUrl;

                    IMatchServer remoteMatchServer = Dependencies.RemoteMatchServer;
                    IMatchServer localMatchServer = Dependencies.LocalMatchServer;

                    if (remoteMatchServer != null && remoteMatchServer.IsConnected)
                    {
                        remoteMatchServer.Activate();
                    }
                    else
                    {
                        localMatchServer.Activate();
                    }

                    IVoxelInputManager inputManager = Dependencies.InputManager;
                    if(inputManager != null)
                    {
                        inputManager.ActivateAll();
                    }
                    
                    if(mapName != "Default")
                    {
                        PlayerPrefs.SetString("lastmap", mapName);
                    }
                
                    callback();
                });
            });
          
        }

        private static void LoginOrSignupCompleted(string mapName, int playerIndex, int botIndex, int playersCount, int botsCount, Action callback, Action<Error> error)
        {
            //IProgressIndicator progress = Dependencies.Progress;
            IGameServer server = Dependencies.GameServer;
            IGlobalSettings gSettings = Dependencies.Settings;

            playerIndex++;

            if (playerIndex == playersCount)
            {
                server.GetMaps(gSettings.ClientId, (e2, maps) =>
                {
                    if (server.HasError(e2))
                    {
                        error(e2);
                       // progress.IsVisible = false;
                        return;
                    }

                    MapInfo mapInfo;
                    if (!string.IsNullOrEmpty(mapName))
                    {
                        mapInfo = maps.Where(m => m.Name == mapName).FirstOrDefault();

                        if (mapInfo == null)
                        {
                            Debug.Log(mapName + " not found. Searching for default map..");
                            mapName = "Default";
                            mapInfo = maps.Where(m => m.Name == mapName).FirstOrDefault();
                            if(mapInfo != null)
                            {
                                Debug.Log("Default map found");
                            }
                        }
                    }
                    else
                    {
                        mapInfo = PlayerPrefs.HasKey("lastmap") ? maps.Where(m => m.Name == PlayerPrefs.GetString("lastmap")).FirstOrDefault() : maps.FirstOrDefault();

                        if(mapInfo == null)
                        {
                            mapInfo = maps.FirstOrDefault();
                        }
                    }

                    if (mapInfo == null)
                    {
                        Debug.LogWarning("No maps. Creating Default map...");
                        CreateDefaultMap((createDefaultError, defaultMapInfo) =>
                        {
                            if (server.HasError(createDefaultError))
                            {
                                error(createDefaultError);
                               //progress.IsVisible = false;
                                return;
                            }

                            mapName = defaultMapInfo.Name;
                            mapInfo = defaultMapInfo;
                            CreateRoom(mapName, playerIndex, botIndex, playersCount, botsCount, callback, error, null /*progress*/, server, gSettings, mapInfo);
                        });
                    }
                    else
                    {
                        mapName = mapInfo.Name;
                        CreateRoom(mapName, playerIndex, botIndex, playersCount, botsCount, callback, error, null /*progress*/, server, gSettings, mapInfo);
                    }
                });
            }
            else
            {
                InitGame(mapName, playerIndex, botIndex, playersCount, botsCount, callback, error);
            }
        }

        private static void CreateRoom(string mapName, int playerIndex, int botIndex, int playersCount, int botsCount, Action callback, Action<Error> error, IProgressIndicator progress, IGameServer server, IGlobalSettings gSettings, MapInfo mapInfo)
        {
            server.CreateRoom(gSettings.ClientId, mapInfo.Id, GameMode.FreeForAll, (e3, room) =>
            {
                if (server.HasError(e3))
                {
                    error(e3);
                    //progress.IsVisible = false;
                    return;
                }

                InitGame(mapName, playerIndex, botIndex, playersCount, botsCount, callback, error);
            });
        }
    }
}
