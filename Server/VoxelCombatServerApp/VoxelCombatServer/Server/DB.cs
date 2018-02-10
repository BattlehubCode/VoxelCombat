using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;

namespace Battlehub.VoxelCombat
{
    public class Map
    {
        public string Property
        {
            get;
            set;
        }

        public string Column
        {
            get;
            set;
        }
    }

    public interface IDb
    {
        DbTransaction Transaction(DbConnection connection);

        DbConnection Connection(DbConnection connection = null);

        DbCommand Command(string text, DbConnection connection, DbTransaction transaction);

        DbParameter Parameter(string parameterName, object value);

        int Identity(DbCommand command);

        int ExecuteInsert(IPersistentObject persistentObject, DbConnection connection, DbTransaction transaction, string sql, params DbParameter[] parameters);

        int ExecuteNonQuery(DbConnection connection, DbTransaction transaction, string sql, params DbParameter[] parameters);

        IEnumerable<T> ExecuteSelect<T>(DbConnection connection, DbTransaction transaction, string sql, Func<DbDataReader, T> getFromReaderFunc, params DbParameter[] parameters)
            where T : new();

        T ExecuteSelectFirst<T>(DbConnection connection, DbTransaction transaction, string sql, Func<DbDataReader, T> getFromReaderFunc, params DbParameter[] parameters)
            where T : IPersistentObject, new();

        object ExecuteScalar(DbConnection connection, DbTransaction transaction, string sql, params DbParameter[] parameters);
    }



    public class Db : IDb
    {
        public static string ConnectionstringName
        {
            get;
            set;
        }

        static Db()
        {
            ConnectionstringName = "LocalSqlServer";
        }

        private static IDb _instance;
        public static IDb Get
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Db();
                }

                return _instance;
            }
        }

        public static IDb Begin
        {
            get
            {
                return Get;
            }
        }

        public DbTransaction Transaction(DbConnection connection)
        {
            return connection.BeginTransaction();
        }

        public DbConnection Connection(DbConnection connection = null)
        {
            if (connection != null)
            {
                return connection;
            }

            connection = new SqlConnection(ConfigurationManager.ConnectionStrings[ConnectionstringName].ConnectionString);
            connection.Open();
            return connection;
        }

        public DbCommand Command(string text, DbConnection connection, DbTransaction transaction)
        {
            return new SqlCommand(text, (SqlConnection)connection, (SqlTransaction)transaction);
        }

        public DbParameter Parameter(string parameterName, object value)
        {
            if (value == null)
            {
                value = DBNull.Value;
            }
            return new SqlParameter(parameterName, value);
        }

        public int Identity(DbCommand command)
        {
            command.Parameters.Clear();
            command.CommandText = "SELECT @@IDENTITY";

            try
            {
                // Get the last inserted id.
                return Convert.ToInt32(command.ExecuteScalar());
            }
            catch (Exception e)
            {
                return -1;
            }

        }

        public int ExecuteInsert(IPersistentObject persistentObject, DbConnection connection, DbTransaction transaction, string sql, params DbParameter[] parameters)
        {
            bool close = connection == null;
            int result = 0;
            try
            {
                connection = Db.Get.Connection(connection);
                using (DbCommand command = Db.Get.Command(sql, connection, transaction))
                {
                    foreach (DbParameter parameter in parameters)
                    {
                        command.Parameters.Add(parameter);
                    }

                    result = command.ExecuteNonQuery();
                    int id = Db.Get.Identity(command);
                    if (id > -1)
                    {
                        persistentObject.Id = id;
                    }
                }
            }
            finally
            {
                if (close && connection != null)
                {
                    connection.Close();
                }
            }
            return result;
        }

        public int ExecuteNonQuery(DbConnection connection, DbTransaction transaction, string sql, params DbParameter[] parameters)
        {
            bool close = connection == null;
            int result = 0;
            try
            {
                connection = Db.Get.Connection(connection);
                using (DbCommand command = Db.Get.Command(sql, connection, transaction))
                {
                    foreach (DbParameter parameter in parameters)
                    {
                        command.Parameters.Add(parameter);
                    }

                    result = command.ExecuteNonQuery();
                }
            }
            finally
            {
                if (close && connection != null)
                {
                    connection.Close();
                }
            }

            return result;
        }


        public T ExecuteSelectFirst<T>(DbConnection connection, DbTransaction transaction, string sql, Func<DbDataReader, T> getFromReaderFunc, params DbParameter[] parameters)
            where T : IPersistentObject, new()
        {
            T result = default(T);
            bool close = connection == null;
            try
            {
                connection = Db.Get.Connection(connection);
                using (DbCommand command = Db.Get.Command(sql, connection, transaction))
                {
                    if (parameters != null)
                    {
                        foreach (DbParameter param in parameters)
                        {
                            command.Parameters.Add(param);
                        }
                    }

                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result = getFromReaderFunc(reader);
                        }

                    }
                }
            }
            finally
            {
                if (close && connection != null)
                {
                    connection.Close();
                }
            }

            return result;
        }

        public IEnumerable<T> ExecuteSelect<T>(
            DbConnection connection,
            DbTransaction transaction,
            string sql,
            Func<DbDataReader, T> getFromReaderFunc,
            params DbParameter[] parameters)
            where T : new()
        {
            bool close = connection == null;
            try
            {
                connection = Db.Get.Connection(connection);
                using (DbCommand command = Db.Get.Command(sql, connection, transaction))
                {
                    if (parameters != null)
                    {
                        foreach (DbParameter param in parameters)
                        {
                            command.Parameters.Add(param);
                        }
                    }

                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return getFromReaderFunc(reader);
                        }

                    }
                }
            }
            finally
            {
                if (close && connection != null)
                {
                    connection.Close();
                }
            }
        }

        public Object ExecuteScalar(DbConnection connection, DbTransaction transaction, string sql, params DbParameter[] parameters)
        {
            bool close = connection == null;
            try
            {
                connection = Db.Get.Connection(connection);
                using (DbCommand command = Db.Get.Command(sql, connection, transaction))
                {
                    if (parameters != null)
                    {
                        foreach (DbParameter param in parameters)
                        {
                            command.Parameters.Add(param);
                        }
                    }

                    return command.ExecuteScalar();
                }
            }
            finally
            {
                if (close && connection != null)
                {
                    connection.Close();
                }
            }
        }
    }
}
