using System;
using System.Linq;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
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

        public static void Init(string mapName, int playersCount, int botsCount, Action callback, Action<Error> error)
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

            IProgressIndicator progress = Dependencies.Progress;
            progress.IsVisible = true;

            InitGame(mapName, 0, 0, playersCount, botsCount, callback, error);
        }

        private static void InitGame(string mapName, int playerIndex, int botIndex, int playersCount, int botsCount, Action callback, Action<Error> error)
        {
            IProgressIndicator progress = Dependencies.Progress;
            IGameServer server = Dependencies.GameServer;
            IGlobalSettings gSettings = Dependencies.Settings;
           
            if(playerIndex < playersCount)
            {
                server.Login(m_playerNames[playerIndex], "", gSettings.ClientId, (e1, playerId) =>
                {
                    if (server.HasError(e1))
                    {
                        error(e1);
                        progress.IsVisible = false;
                        server.SignUp(m_playerNames[playerIndex], "", gSettings.ClientId, (e100, p) =>
                        {
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
                    Launch(callback, error, progress, server, gSettings);
                }
                else
                {
                    server.CreateBot(gSettings.ClientId, m_playerNames[playersCount + botIndex], BotType.Hard, (e4, botId, roomWithBot) =>
                    {
                        if (server.HasError(e4))
                        {
                            error(e4);
                            progress.IsVisible = false;
                            return;
                        }

                        botIndex++;

                        if (botIndex == botsCount)
                        {
                            Launch(callback, error, progress, server, gSettings);
                        }
                        else
                        {
                            InitGame(mapName, playerIndex, botIndex, playersCount, botsCount, callback, error);
                        }
                    });
                }
            }
        }

        private static void Launch(Action callback, Action<Error> error, IProgressIndicator progress, IGameServer server, IGlobalSettings gSettings)
        {
            server.Launch(gSettings.ClientId, (e5, serverUrl) =>
            {
                if (server.HasError(e5))
                {
                    error(e5);
                    progress.IsVisible = false;
                    return;
                }

                progress.IsVisible = false;

                LocalMatchServer localMatchServer = UnityEngine.Object.FindObjectOfType<LocalMatchServer>();
                if (localMatchServer != null)
                {
                    localMatchServer.Init();
                }

                Dependencies.InputManager.ActivateAll();

                callback();
            });
        }

        private static void LoginOrSignupCompleted(string mapName, int playerIndex, int botIndex, int playersCount, int botsCount, Action callback, Action<Error> error)
        {
            IProgressIndicator progress = Dependencies.Progress;
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
                        progress.IsVisible = false;
                        return;
                    }

                    MapInfo mapInfo;
                    if (!string.IsNullOrEmpty(mapName))
                    {
                        mapInfo = maps.Where(m => m.Name == mapName).FirstOrDefault();
                    }
                    else
                    {
                        mapInfo = PlayerPrefs.HasKey("lastmap") ? maps.Where(m => m.Name == PlayerPrefs.GetString("lastmap")).FirstOrDefault() : maps.FirstOrDefault();
                    }

                    if (mapInfo == null)
                    {
                        Debug.LogWarning("No maps");
                        progress.IsVisible = false;
                        return;
                    }

                    server.CreateRoom(gSettings.ClientId, mapInfo.Id, GameMode.FreeForAll, (e3, room) =>
                    {
                        if (server.HasError(e3))
                        {
                            error(e3);
                            progress.IsVisible = false;
                            return;
                        }

                        InitGame(mapName, playerIndex, botIndex, playersCount, botsCount, callback, error);
                    });
                });
            }
            else
            {
                InitGame(mapName, playerIndex, botIndex, playersCount, botsCount, callback, error);
            }
        }
    }
}
