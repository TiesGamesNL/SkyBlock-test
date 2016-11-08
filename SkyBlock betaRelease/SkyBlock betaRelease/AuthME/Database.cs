using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using MySql.Data.MySqlClient;
using AuthME;
using log4net;

namespace AuthME
{
    public class Database
    {
        static ILog Log = LogManager.GetLogger(typeof(AuthME));

        private MySqlConnection mysql_connect;

        private string Connect;

        public Database()
        {
            Connect = "server=localhost;" +
                "database=test";// + 
                                //"uid=darklex;" +
                                //"pwd=kfijd2001;" +
                                //"Pooling=true;" +
                                //"Min Pool Size=0;" +
                                //"Max Pool Size=100;" +
                                //"Connection Lifetime=0";
        }

        public void open()
        {
            mysql_connect = new MySqlConnection(Connect);
            try
            {
                Log.DebugFormat("False ! ");
                mysql_connect.Open();
                Log.DebugFormat("Connection Open ! ");
                Log.DebugFormat("True ! ");
                //mysql_connect.Close();
            }
            catch (Exception ex)
            {
                Log.Error("Can not open connection ! \n\n end with:  " + ex);
            }
            Log.DebugFormat("Database loaded! ");
        }

        /*public void open()
        {
			Connect = "server=127.0.0.1;" +
					"database=pocketcraft;uid=root;" +
					"pwd=8951330q;" +
					"Pooling=true;" +
					"Min Pool Size=0;" +
					"Max Pool Size=100;" +
					"Connection Lifetime=0";
			mysql_connect = new MySqlConnection(Connect);
			try
			{
				mysql_connect.Open();
				Log.Info("Connection Open ! ");
				//mysql_connect.Close();
			}
			catch (Exception ex)
			{
				Log.Info("Can not open connection ! ");
			}
        }*/

        public void close()
        {
            mysql_connect.Close();
            Log.Info("Connection Open ! ");
        }

        public void Insert(string query)
        {
            //create command and assign the query and connection from the constructor
            MySqlCommand cmd = new MySqlCommand(query, mysql_connect);
            cmd.ExecuteNonQuery(); ;
        }

        public List<object[]> ExecuteQuery(string query)
        {
            List<object[]> rows = new List<object[]>();
            try
            {
                MySqlCommand cmd = new MySqlCommand(query, mysql_connect);

                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    object[] row = new object[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[i] = reader[i];

                    }
                    rows.Add(row);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            return rows;
        }

        //Select statement
        public List<string>[] Select(string query)
        {

            //Create a list to store the result

            MySqlCommand cmd = new MySqlCommand(query, mysql_connect);
            MySqlDataReader dataReader = cmd.ExecuteReader();
            List<string>[] rows = new List<string>[dataReader.FieldCount];
            while (dataReader.Read())
            {
                for (int i = 0; i < dataReader.FieldCount; i++)
                {
                    rows[i].Add(dataReader[i] + "");

                }
            }

            dataReader.Close();
            return rows;
        }

        public void Update(string query)
        {
            //create mysql command
            MySqlCommand cmd = new MySqlCommand();
            //Assign the query using CommandText
            cmd.CommandText = query;
            //Assign the connection using Connection
            cmd.Connection = mysql_connect;
            //Execute query
            cmd.ExecuteNonQuery();
        }

        public static int UnixTime()
        {

            int unixtime = Convert.ToInt32((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds);

            return unixtime;

        }
    }
}

