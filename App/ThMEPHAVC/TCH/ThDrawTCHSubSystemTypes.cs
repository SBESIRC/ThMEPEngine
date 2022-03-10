namespace ThMEPHVAC.TCH
{
    public class ThDrawTCHSubSystemTypes
    {
        private TCHSubSystemParam subSystemParam;
        private ThSQLiteHelper sqliteHelper;

        public ThDrawTCHSubSystemTypes(ThSQLiteHelper sqliteHelper)
        {
            this.sqliteHelper = sqliteHelper;
        }

        public void InsertSubSystem(ref ulong gId)
        {
            sqliteHelper.Conn();
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

                sqliteHelper.Query<TCHSubSystemParam>(recordDuct);
            }
            sqliteHelper.db.Close();
        }
    }
}
