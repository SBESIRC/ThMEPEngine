﻿namespace ThMEPHVAC.TCH
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
            materialsParam = new TCHMaterialsParam()
            {
                ID = gId++,
                Name = "钢板风道",
                type = 1,
            };
            string recordDuct = $"INSERT INTO " + ThTCHCommonTables.materialsTableName +
                                 " VALUES ('" + materialsParam.ID.ToString() + "'," +
                                 "'" + materialsParam.Name + "'," +
                                 "'" + materialsParam.type.ToString() + "')";
            sqliteHelper.Conn();
            sqliteHelper.Query<TCHMaterialsParam>(recordDuct);
            sqliteHelper.db.Close();
        }
    }
}