using System.Collections;
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
        private List<T> DataTableToModels<T>(DataTable dt) where T:class 
        {
            var arrayList = new ArrayList();
            foreach (DataRow dataRow in dt.Rows)
            {
                var dictionary = new Dictionary<string, object>();
                foreach (DataColumn dataColumn in dt.Columns)
                {
                    var s_value = dataRow[dataColumn.ColumnName];
                    dictionary.Add(dataColumn.ColumnName, s_value);
                }
                arrayList.Add(dictionary);
            }
            var json = JsonHelper.SerializeObject(arrayList);
            return JsonHelper.DeserializeJsonToList<T>(json);
        }
    }
}
