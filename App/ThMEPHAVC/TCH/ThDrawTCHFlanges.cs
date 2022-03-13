using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPHVAC.TCH
{
    public class ThDrawTCHFlanges
    {
        private TCHFlangesParam flangesParam;
        private ThSQLiteHelper sqliteHelper;

        public ThDrawTCHFlanges(ThSQLiteHelper sqliteHelper)
        {
            this.sqliteHelper = sqliteHelper;
        }

        public void Draw(Point3d p, Vector3d dirVec, double width, double height,ref ulong gId)
        {
#if false
            sqliteHelper.Conn();
            RecordFlangesInfo(ref gId);
            var gap = ThTCHCommonTables.flgThickness * 0.5;
            var param1 = new TCHInterfaceParam()
            {
                ID = flangesParam.startFaceID,
                sectionType = 0,
                height = height,
                width = width,
                normalVector = dirVec,
                heighVector = new Vector3d(0, 0, 1),
                centerPoint = p + (dirVec * gap)
            };
            var param2 = new TCHInterfaceParam()
            {
                ID = flangesParam.endFaceID,
                height = height,
                width = width,
                normalVector = dirVec,
                heighVector = new Vector3d(0, 0, 1),
                centerPoint = p - (dirVec * gap)
            };
            ThTCHService.RecordPortInfo(sqliteHelper, new List<TCHInterfaceParam>() { param1, param2 });
            sqliteHelper.db.Close();
#endif
        }

        private void RecordFlangesInfo(ref ulong gId)
        {
            flangesParam = new TCHFlangesParam()
            {
                ID = gId++,
                startFaceID = gId++,
                endFaceID = gId++,
                subSystemID = 1,
                materialID = 0,
                type = 1,
                thickness = ThTCHCommonTables.flgThickness,
                skirtSize = 30
            };
            string record = $"INSERT INTO " + ThTCHCommonTables.flangesTableName +
                          " VALUES ('" + flangesParam.ID.ToString() + "'," +
                                  "'" + flangesParam.startFaceID.ToString() + "'," +
                                  "'" + flangesParam.endFaceID.ToString() + "'," +
                                  "'" + flangesParam.subSystemID.ToString() + "'," +
                                  "'" + flangesParam.materialID.ToString() + "'," +
                                  "'" + flangesParam.type.ToString() + "'," +
                                  "'" + flangesParam.thickness.ToString() + "'," +
                                  "'" + flangesParam.skirtSize.ToString() + "')";
            sqliteHelper.Query<TCHFlangesParam>(record);
        }
    }
}
