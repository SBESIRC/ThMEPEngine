using System.Collections.Generic;
using System.Data;
using System.IO;
using ThMEPEngineCore.IO.JSON;
using ThMEPIO.DB.SQLite;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;

namespace ThMEPTCH.TCHArchDataConvert
{
    class TCHArchDBData
    {
        private string tchDBPath;
        protected THMEPSQLiteServices dbHelper;
        public TCHArchDBData(string dbPath) 
        {
            tchDBPath = dbPath;
            InitTCHDatabase();
        }
        protected void InitTCHDatabase()
        {
            if (string.IsNullOrEmpty(tchDBPath) || !File.Exists(tchDBPath))
                return;
            dbHelper = new THMEPSQLiteServices(tchDBPath);
        }
        public List<TArchEntity> AllTArchEntitys() 
        {
            var resList = new List<TArchEntity>();
            var allWalls = GetDBWallDatas();
            resList.AddRange(allWalls);
            var allWindows = GetDBWindowDatas();
            resList.AddRange(allWindows);
            var allDoors = GetDBDoorDatas();
            resList.AddRange(allDoors);
            return resList;
        }
        public List<TArchWall> GetDBWallDatas() 
        {
            var walls = new List<TArchWall>();
            var sql = TCHArchSQL.GetWallSQL();
            var dataTable = dbHelper.GetTable(sql);
            if (dataTable == null || dataTable.Rows == null || dataTable.Rows.Count < 1)
                return walls;
            walls = DataTableToModels<TArchWall>(dataTable);
            return walls;
        }
        public List<TArchDoor> GetDBDoorDatas()
        {
            var doors = new List<TArchDoor>();
            var sql = TCHArchSQL.GetDoorSQL();
            var dataTable = dbHelper.GetTable(sql);
            if (dataTable == null || dataTable.Rows == null || dataTable.Rows.Count < 1)
                return doors;
            doors = DataTableToModels<TArchDoor>(dataTable);
            return doors;
        }
        public List<TArchWindow> GetDBWindowDatas()
        {
            var windows = new List<TArchWindow>();
            var sql = TCHArchSQL.GetWindowSQL();
            var dataTable = dbHelper.GetTable(sql);
            if (dataTable == null || dataTable.Rows == null || dataTable.Rows.Count < 1)
                return windows;
            windows = DataTableToModels<TArchWindow>(dataTable);
            return windows;
        }
        List<T> DataTableToModels<T>(DataTable dataTable) where T:class 
        {
            var jsonStr = JsonHelper.ToJson(dataTable);
            var resList = JsonHelper.DeserializeJsonToList<T>(jsonStr);
            return resList;
        }
    }
}
