using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;

namespace GraduatedProject_Server
{
    public interface ISQL : IDisposable
    {
        public void Initilize();
        public bool Select(string query, out MySqlDataReader? reader);
        public bool SelectEnd(ref MySqlDataReader? reader);
        public bool Query(string query);
    }

}
