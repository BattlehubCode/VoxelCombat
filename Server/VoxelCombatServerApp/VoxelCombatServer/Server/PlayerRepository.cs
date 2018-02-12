using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Battlehub.VoxelCombat
{
    public interface IPlayerRepository
    {
        void CreatePlayer(Guid guid, string name, string password, Action<Error, Player, byte[]> callback);
        void GetPlayer(string name, string password, Action<Error, Player, byte[]> callback);
        void GetPlayer(string name, byte[] pwdHash, Action<Error, Player> callback);
        void GetPlayers(Guid[] guids, Action<Error, Dictionary<Guid, Player>> callback);
    }

    public class PlayerRepository : IPlayerRepository
    {
        private const int SALT_LENGTH = 24;
        private const int HASH_LENGTH = 24;
        private const int ITERATIONS = 2000;

        private byte[] GenerateSalt(int length)
        {
            var bytes = new byte[length];

            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(bytes);
            }

            return bytes;
        }

        private byte[] GenerateHash(byte[] password, byte[] salt, int iterations, int length)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                return deriveBytes.GetBytes(length);
            }
        }

        private static bool Equals(byte[] a, byte[] b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
                diff |= (uint)(a[i] ^ b[i]);
            return diff == 0;
        }

        private class PlayerCredentials : IPersistentObject
        {
            public byte[] PasswordHash;
            public byte[] Salt;
            public int Iterations;

            public int Id
            {
                get;set;
            }
        }

        private static PlayerCredentials GetCredentials(string name)
        {
            const string sql = "SELECT Password, Salt, Iterations FROM Player WHERE Name = @Name";

            return Db.Begin.ExecuteSelectFirst(null, null, sql, reader =>
            {
                return new PlayerCredentials
                {
                    PasswordHash = (byte[])reader["Password"],
                    Salt = (byte[])reader["Salt"],
                    Iterations = (int)reader["Iterations"],
                };
            }, 
            Db.Get.Parameter("@Name", name));
        }

        private void ValidateUser(string name, byte[] passwordHash, Action<PlayerCredentials> callback)
        {
            PlayerCredentials credetials = GetCredentials(name);
            if(credetials == null)
            {
                callback(null);
                return;
            }
            callback(Equals(credetials.PasswordHash, passwordHash) ? credetials : null);
        }

        private void ValidateUser(string name, string password, Action<PlayerCredentials> callback)
        {
            PlayerCredentials credetials = GetCredentials(name);
            if (credetials == null)
            {
                callback(null);
                return;
            }

            byte[] passwordHash = GenerateHash(Encoding.UTF8.GetBytes(password), credetials.Salt, credetials.Iterations, HASH_LENGTH);
            callback(Equals(credetials.PasswordHash, passwordHash) ? credetials : null);
        }

        public void CreatePlayer(Guid guid, string name, string password, Action<Error, Player, byte[]> callback)
        {
            PlayerCredentials credentials = GetCredentials(name);
            if(credentials != null)
            {
                callback(new Error(StatusCode.AlreadyExists), null, null);
                return;
            }

            if(string.IsNullOrWhiteSpace(password) || password.Length < 1)
            {
                callback(new Error(StatusCode.NotAllowed) { Message = "Password required" }, null, new byte[0]);
                return;
            }

            byte[] salt = GenerateSalt(SALT_LENGTH);
            byte[] pwd = GenerateHash(Encoding.UTF8.GetBytes(password), salt, ITERATIONS, HASH_LENGTH);
           
            const string sql = "INSERT INTO Player(PlayerId, Name, Password, Salt, Iterations) VALUES (@PlayerId, @Name, @Password, @Salt, @Iterations)";
            try
            {
                Db.Begin.ExecuteNonQuery(null, null, sql,
                    Db.Get.Parameter("@PlayerId", guid),
                    Db.Get.Parameter("@Name", name),
                    Db.Get.Parameter("@Password", pwd),
                    Db.Get.Parameter("@Salt", salt),
                    Db.Get.Parameter("@Iterations", ITERATIONS));
                callback(new Error(StatusCode.OK),
                    new Player
                    {
                        Name = name,
                        BotType = BotType.None,
                        Id = guid,
                        Victories = 0,
                    }, 
                    pwd);
            }
            catch(Exception e)
            {
                callback(new Error(StatusCode.UnhandledException) { Message = e.ToString() }, null, new byte[0]);
            }
        }

        public void GetPlayer(string name, string password, Action<Error, Player, byte[]> callback)
        {
            ValidateUser(name, password, credentials =>
            {
                if (credentials == null)
                {
                    callback(new Error(StatusCode.NotAuthenticated), null, new byte[0]);
                    return;
                }

                Player player = GetPlayer(name);
                callback(new Error(StatusCode.OK), player, credentials.PasswordHash);
            });
        }

        public void GetPlayer(string name, byte[] pwdHash, Action<Error, Player> callback)
        {
            ValidateUser(name, pwdHash, credentials =>
            {
                if (credentials == null)
                {
                    callback(new Error(StatusCode.NotAuthenticated), null);
                    return;
                }

                Player player = GetPlayer(name);
                callback(new Error(StatusCode.OK), player);
            });
        }

        private static Player GetPlayer(string name)
        {
            const string sql = "SELECT PlayerId, Name FROM Player WHERE Name = @Name";
            return Db.Begin.ExecuteSelectFirst(null, null, sql, reader =>
            {
                return new Player
                {
                    Id = (Guid)reader["PlayerId"],
                    Name = (string)reader["Name"],
                    BotType = BotType.None
                };
            },
            Db.Get.Parameter("@Name", name));
        }

        public void GetPlayers(Guid[] guids, Action<Error, Dictionary<Guid, Player>> callback)
        {
            string sql = "SELECT PlayerId, Name FROM Player WHERE PlayerId IN(";
            for(int i = 0; i < guids.Length - 1; ++i)
            {
                sql += "'" + guids[i] + "',";
            }
            if(guids.Length > 0)
            {
                sql += "'" + guids[guids.Length - 1] + "')";
            }

            Dictionary<Guid, Player> players = Db.Begin.ExecuteSelect(null, null, sql, reader =>
            {
                return new Player
                {
                    Id = (Guid)reader["PlayerId"],
                    Name = (string)reader["Name"],
                    BotType = BotType.None
                };
            }).ToDictionary(p => p.Id);

            callback(new Error(StatusCode.OK), players);
        }

      
    }
}
