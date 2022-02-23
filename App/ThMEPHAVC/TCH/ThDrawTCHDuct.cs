using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.TCH
{
    public class ThDrawTCHDuct
    {
        private TCHDuctParam ductParam;
        private ThSQLiteHelper sqliteHelper;

        public ThDrawTCHDuct(ThSQLiteHelper sqliteHelper)
        {
            this.sqliteHelper = sqliteHelper;
        }

        public void Draw(List<SegInfo> segInfos, Matrix3d mat, ref ulong gId)
        {
            sqliteHelper.Conn();
            var gap = ThTCHCommonTables.flgThickness * 0.5;
            foreach (var seg in segInfos)
            {
                RecordDuctInfo(seg.airVolume, ref gId);
                GetWidthAndHeight(seg.ductSize, out double width, out double height);
                var l = seg.GetShrinkedLine();
                var dirVec = (l.EndPoint - l.StartPoint).GetNormal();
                var sEndParam = new TCHInterfaceParam()
                {
                    ID = ductParam.startFaceID,
                    sectionType = ductParam.sectionType,
                    height = height,
                    width = width,
                    normalVector = dirVec,
                    heighVector = new Vector3d(0, 0, 1),
                    centerPoint = l.StartPoint.TransformBy(mat) + (gap * dirVec),
                };
                var eEndParam = new TCHInterfaceParam()
                {
                    ID = ductParam.endFaceID,
                    sectionType = ductParam.sectionType,
                    height = height,
                    width = width,
                    normalVector = -dirVec,
                    heighVector = new Vector3d(0, 0, 1),
                    centerPoint = l.EndPoint.TransformBy(mat) - (gap * dirVec)
                };
                ThTCHService.RecordPortInfo(sqliteHelper, new List<TCHInterfaceParam>() { sEndParam, eEndParam });
            }
            sqliteHelper.db.Close();
        }
        private void RecordDuctInfo(double airVolume, ref ulong gId)
        {
            ductParam = new TCHDuctParam()
            {
                ID = gId++,
                startFaceID = gId++,
                endFaceID = gId++,
                subSystemID = 1,
                materialID = 0,
                sectionType = 0,
                ductType = 1,
                Soft = 0,
                Bulge = 0.0,
                AirLoad = airVolume
            };
            string recordDuct = $"INSERT INTO " + ThTCHCommonTables.ductTableName +
                          " VALUES ('" + ductParam.ID.ToString() + "'," +
                                  "'" + ductParam.startFaceID.ToString() + "'," +
                                  "'" + ductParam.endFaceID.ToString() + "'," +
                                  "'" + ductParam.subSystemID.ToString() + "'," +
                                  "'" + ductParam.materialID.ToString() + "'," +
                                  "'" + ductParam.sectionType.ToString() + "'," +
                                  "'" + ductParam.ductType.ToString() + "'," +
                                  "'" + ductParam.Soft.ToString() + "'," +
                                  "'" + ductParam.Bulge.ToString() + "'," +
                                  "'" + ductParam.AirLoad.ToString() + "')";
            sqliteHelper.Query<TCHDuctParam>(recordDuct);
        }
        private static void GetWidthAndHeight(string size, out double width, out double height)
        {
            string[] s = size.Split('x');
            if (s.Length != 2)
                throw new NotImplementedException("Duct size info doesn't contain width or height");
            width = Double.Parse(s[0]);
            height = Double.Parse(s[1]);
        }
    }
}
