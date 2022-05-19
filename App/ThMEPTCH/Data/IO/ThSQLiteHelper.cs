using SQLite;
using System.Collections.Generic;

namespace ThMEPTCH.Data.IO
{
    public class ThSQLiteHelper
    {
        private string path;
        public SQLiteConnection db;
        public ThSQLiteHelper(string path)
        {
            this.path = path;
        }
        public void Conn()
        {
            db = new SQLiteConnection(path);
        }
        public void CloseConnect(bool delConnect=false)
        {
            if (db == null)
                return;
            db.Close();
            if (delConnect)
                db = null;
        }

        public void ClearTable(string tableName)
        {
            string s = "DELETE FROM " + tableName;
            Execute(s);
        }

        public int Add<T>(T model)
        {
            return db.Insert(model);
        }

        public int Update<T>(T model)
        {
            return db.Update(model);
        }

        public int Delete<T>(T model)
        {
            return db.Update(model);
        }
        public List<T> Query<T>(string sql) where T : new()
        {
            return db.Query<T>(sql);
        }

        public int Execute(string sql)
        {
            return db.Execute(sql);
        }
    }
}
