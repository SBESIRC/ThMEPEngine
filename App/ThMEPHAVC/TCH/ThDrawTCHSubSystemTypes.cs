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
            subSystemParam = new TCHSubSystemParam()
            {
                ID = gId++,
                Name = "新风",
                remark = "",
            };
            string recordDuct = $"INSERT INTO " + ThTCHCommonTables.subSystemTypeTableName +
                                 " VALUES ('" + subSystemParam.ID.ToString() + "'," +
                                 "'" + subSystemParam.Name + "'," +
                                 "'" + subSystemParam.remark + "')";
            sqliteHelper.Conn();
            sqliteHelper.Query<TCHSubSystemParam>(recordDuct);
            sqliteHelper.db.Close();
        }
    }
}
