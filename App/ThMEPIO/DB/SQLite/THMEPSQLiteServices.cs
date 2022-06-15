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

        public DataTable ExecuteDataTable(string tableName)
        {
            var sql = $"SELECT * FROM {tableName};";
            return GetTable(sql);
        }

        public DataTable GetTable(string sql)
        {
            return SQLiteHelper.ExecuteDataTable(_connecttion, sql);
        }

        public int ExecuteNonQuery(string sql)
        {
            return SQLiteHelper.ExecuteNonQuery(_connecttion, sql);
        }
    }
}
