using ThMEPIO.DB.SQLite;

namespace ThMEPHVAC.TCH
{
    public class ThDrawTCHSubSystemTypes
    {
        private TCHSubSystemParam subSystemParam;
        private THMEPSQLiteServices sqliteHelper;

        public ThDrawTCHSubSystemTypes(THMEPSQLiteServices sqliteHelper)
        {
            this.sqliteHelper = sqliteHelper;
        }

        public void InsertSubSystem(ref ulong gId)
        {
            foreach (string subSys in ThTCHCommonTables.subSystems)
            {
                subSystemParam = new TCHSubSystemParam()
                {
                    ID = gId++,
                    Name = subSys,
                    remark = "",
                };
                string recordDuct = $"INSERT INTO " + ThTCHCommonTables.subSystemTypeTableName +
                                     " VALUES ('" + subSystemParam.ID.ToString() + "'," +
                                     "'" + subSystemParam.Name + "'," +
                                     "'" + subSystemParam.remark + "')";

                sqliteHelper.ExecuteNonQuery(recordDuct);
            }
            sqliteHelper.CloseConnect();
        }
    }
}
