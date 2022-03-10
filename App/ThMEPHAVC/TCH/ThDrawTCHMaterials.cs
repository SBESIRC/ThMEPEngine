namespace ThMEPHVAC.TCH
{
    public class ThDrawTCHMaterials
    {
        private TCHMaterialsParam materialsParam;
        private ThSQLiteHelper sqliteHelper;

        public ThDrawTCHMaterials(ThSQLiteHelper sqliteHelper)
        {
            this.sqliteHelper = sqliteHelper;
        }

        public void InsertMaterials(ref ulong gId)
        {
            sqliteHelper.Conn();
            foreach (string material in ThTCHCommonTables.materials)
            {
                materialsParam = new TCHMaterialsParam()
                {
                    ID = gId++,
                    Name = material,
                    type = 1,
                };
                string recordDuct = $"INSERT INTO " + ThTCHCommonTables.materialsTableName +
                                     " VALUES ('" + materialsParam.ID.ToString() + "'," +
                                     "'" + materialsParam.Name + "'," +
                                     "'" + materialsParam.type.ToString() + "')";
                var a = sqliteHelper.Query<TCHMaterialsParam>(recordDuct);
            }
            sqliteHelper.db.Close();
        }
    }
}