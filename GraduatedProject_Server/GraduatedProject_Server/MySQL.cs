using System.Collections;
using System.Collections.Generic;
using System;
using MySql.Data.MySqlClient;
using MyPacket;

namespace GraduatedProject_Server
{
    public struct Query
    {
        public string query { get; private set; }

        static readonly string selectQuery = "SELECT <COLUMNS> FROM <TABLES> [WHERE];";
        static readonly string deleteQuery = "DELETE FROM <TABLES> [WHERE];";
        static readonly string updateQuery = "UPDATE <TABLES> SET [CHANGE] [WHERE];";
        static readonly string insertQuery = "INSERT INTO <TABLES> [COLUMNS] [VALUES];";

        public string Select(string columns, string tables, string where = "")
        {
            query = selectQuery;
            query = query.Replace("<COLUMNS>", columns);
            query = query.Replace("<TABLES>", tables);

            if (where == "") query = query.Replace("[WHERE]", $"");
            else query = query.Replace("[WHERE]", $"WHERE {where}");

            return query;
        }

        public string Delete(string tables, string where = "")
        {
            query = deleteQuery;
            query = query.Replace("<TABLES>", tables);

            if (where == "") query = query.Replace("[WHERE]", $"");
            else query = query.Replace("[WHERE]", $"WHERE {where}");

            return query;
        }

        public string Update(string tables, string change, string where = "")
        {
            query = updateQuery;
            query = query.Replace("<TABLES>", tables);
            query = query.Replace("[CHANGE]", change);

            if (where == "") query = query.Replace("[WHERE]", $"");
            else query = query.Replace("[WHERE]", $"WHERE {where}");

            return query;
        }

        public string Insert(string tables, string values)
        {
            query = insertQuery;
            query = query.Replace("<TABLES>", tables);
            query = query.Replace("[COLUMNS]", "");
            query = query.Replace("[VALUES]", $"VALUES({values})");
            return query;
        }
        public string Insert(string tables, string columns, string values)
        {
            query = insertQuery;
            query = query.Replace("<TABLES>", tables);
            query = query.Replace("[COLUMNS]", $"({columns})");
            query = query.Replace("[VALUES]", $"VALUES({values})");
            return query;
        }
    }

    public class MySQL : ISQL
    {
        //readonly string server = "Server=119.196.245.41;";
        readonly string server = "Server=127.0.0.1;";
        readonly string port = "Port=3306;";
        readonly string db = "Database=db;";
        readonly string id = "Uid=root;";
        readonly string pw = "Pwd=Rnfqjf2671!@#;";

        MySqlConnection? connection;

        public void Dispose()
        {
            connection!.Close();
            connection.Dispose();
            GC.SuppressFinalize(this);
        }

        public MySQL()
        {
            Initilize();
        }

        public void Initilize()
        {
            connection = new MySqlConnection($"{server}{port}{db}{id}{pw}");
            connection.Open();
        }

        public bool Query(string query)
        {
            using MySqlCommand cmd = new MySqlCommand(query, connection);
            return cmd.ExecuteNonQuery() == 1;
        }

        public bool Select(string query, out MySqlDataReader? reader)
        {
            using MySqlCommand cmd = new MySqlCommand(query, connection);
            reader = cmd.ExecuteReader();
            return reader.HasRows;
        }

        public bool SelectEnd(ref MySqlDataReader? reader)
        {
            reader?.Close();
            reader?.Dispose();
            reader = null;
            return true;
        }
    }
}