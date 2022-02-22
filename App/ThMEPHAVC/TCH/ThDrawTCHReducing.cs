using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.TCH
{
    public class ThDrawTCHReducing
    {
        private ThSQLiteHelper sqliteHelper;
        private TCHReducingParam reducingParam;
        private ThDrawTCHFlanges flangesService;

        public ThDrawTCHReducing(ThSQLiteHelper sqliteHelper)
        {
            this.sqliteHelper = sqliteHelper;
            flangesService = new ThDrawTCHFlanges(sqliteHelper);
        }

        public void Draw(List<LineGeoInfo> reducingInfos, Matrix3d mat, ref ulong gId)
        {
            sqliteHelper.Conn();
            foreach (var reducing in reducingInfos)
            {
                RecordDuctInfo(ref gId);
                GetReducingInfo(reducing, mat, out Point3d srtP, out Point3d endP, out double bigWidth, out double smallWidth);
                var dirVec = (endP - srtP).GetNormal();
                var gap = ThTCHCommonTables.flgThickness * 0.5;
                var sEndParam = new TCHInterfaceParam()
                {
                    ID = reducingParam.startFaceID,
                    sectionType = 0,
                    height = 0,
                    width = bigWidth,
                    normalVector = dirVec,
                    heighVector = new Vector3d(0, 0, 1),
                    centerPoint = srtP + (gap * dirVec)
                };
                var eEndParam = new TCHInterfaceParam()
                {
                    ID = reducingParam.endFaceID,
                    sectionType = 0,
                    height = 0,
                    width = smallWidth,
                    normalVector = -dirVec,
                    heighVector = new Vector3d(0, 0, 1),
                    centerPoint = endP - (gap * dirVec)
                };
                flangesService.Draw(srtP, dirVec, bigWidth, ref gId);
                flangesService.Draw(endP, -dirVec, smallWidth, ref gId);
                ThTCHService.RecordPortInfo(sqliteHelper, new List<TCHInterfaceParam>() { sEndParam, eEndParam });
            }
            sqliteHelper.db.Close();
        }

        private void GetReducingInfo(LineGeoInfo reducing, Matrix3d mat, out Point3d srtP, out Point3d endP, out double bigWidth, out double smallWidth)
        {
            var l = reducing.centerLines[0] as Line;
            srtP = l.StartPoint.TransformBy(mat);
            endP = l.EndPoint.TransformBy(mat);
            var flg1 = reducing.flg[0] as Line;
            var flg2 = reducing.flg[1] as Line;
            bigWidth = flg1.Length - 90;
            smallWidth = flg2.Length - 90;
        }

        private void RecordDuctInfo(ref ulong gId)
        {
            reducingParam = new TCHReducingParam()
            {
                ID = gId++,
                endFaceID = gId++,
                startFaceID = gId++,
                subSystemID = 1,
                materialID = 0
            };
            string recordDuct = $"INSERT INTO " + ThTCHCommonTables.reducingTableName +
                          " VALUES ('" + reducingParam.ID.ToString() + "'," +
                                  "'" +  reducingParam.startFaceID.ToString() + "'," +
                                  "'" +  reducingParam.endFaceID.ToString() + "'," +
                                  "'" +  reducingParam.subSystemID.ToString() + "'," +
                                  "'" +  reducingParam.materialID.ToString() + "')";
            sqliteHelper.Query<TCHReducingParam>(recordDuct);
        }
    }
}