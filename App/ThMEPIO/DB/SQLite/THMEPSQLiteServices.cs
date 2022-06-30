using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace ThMEPIO.DB.SQLite
{
    public class THMEPSQLiteServices : THMEPDBCoreServices
    {
        private SQLiteConnection _connecttion;
        public THMEPSQLiteServices(string filePath)
        {
            _connecttion = new SQLiteConnection($"Data Source = {filePath};Version = 3;");
        }
        public void ClearTables(List<string> tableNames) 
        {
            foreach (var name in tableNames) 
            {
                ClearTable(name);
            }
        }
        public void ClearTable(string tableName)
        {
            var sql = $"DELETE FROM {tableName}";
            ExecuteNonQuery(sql);
        }
        public void CloseConnect() 
        {
            if (null == _connecttion || _connecttion.State == ConnectionState.Closed)
                return;
            _connecttion.Close();
        }

        public DataTable ExecuteDataTable(string tableName)
        {
            var sql = $"SELECT * FROM {tableName};";
            return GetTable(sql);
        }

        public DataTable GetTable(string sql)
        {
            return ThMEPSQLiteHelper.ExecuteDataTable(_connecttion, sql);
        }

        public int ExecuteNonQuery(string sql)
        {
            return ThMEPSQLiteHelper.ExecuteNonQuery(_connecttion, sql);
        }
    }
}
