using ThMEPIO.DB.SQLite;

namespace ThMEPHVAC.TCH
{
    public class ThDrawTCHMaterials
    {
        private TCHMaterialsParam materialsParam;
        private THMEPSQLiteServices sqliteHelper;

        public ThDrawTCHMaterials(THMEPSQLiteServices sqliteHelper)
        {
            this.sqliteHelper = sqliteHelper;
        }

        public void InsertMaterials(ref ulong gId)
        {
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
                var a = sqliteHelper.ExecuteNonQuery(recordDuct);
            }
            sqliteHelper.CloseConnect();
        }
    }
}