using System;
using System.Collections.Generic;

namespace Battlehub.VoxelCombat
{
    public interface IDB
    {
        void CreatePlayer(Guid guid, string name, string password, Action<Error, Player> callback);
        void GetPlayer(string name, string password, Action<Error, Player> callback);
        void GetPlayers(Guid[] guids, Action<Error, Dictionary<Guid, Player>> callback);
    }

    public class InMemoryDB : IDB
    {
        private readonly Dictionary<string, Player> m_playersByName = new Dictionary<string, Player>();
        private readonly Dictionary<Guid, Player> m_playersByGuid = new Dictionary<Guid, Player>();

        public void CreatePlayer(Guid guid, string name, string password, Action<Error, Player> callback)
        {
            Player player = new Player
            {
                Id = guid,
                BotType = BotType.None,
                Name = name,
                Victories = 0
            };

            m_playersByName.Add(name, player);
            m_playersByGuid.Add(guid, player); 
            callback(new Error(StatusCode.OK), player);
        }

        public void GetPlayer(string name, string password, Action<Error, Player> callback)
        {
            Player player;
            if(m_playersByName.TryGetValue(name, out player))
            {
                callback(new Error(StatusCode.OK), player);
            }
            else
            {
                callback(new Error(StatusCode.OK), null);
            }
        }
        public void GetPlayers(Guid[] guids, Action<Error, Dictionary<Guid, Player>> callback)
        {
            Dictionary<Guid, Player> players = new Dictionary<Guid, Player>();
            for (int i = 0; i < guids.Length; ++i)
            {
                Player player;
                if(m_playersByGuid.TryGetValue(guids[i], out player))
                {
                    players.Add(guids[i], player);
                }
            }

            callback(new Error(StatusCode.OK), players);
        }
    }
}
