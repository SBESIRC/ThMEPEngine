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
            sqliteHelper.ClearTable(ThTCHCommonTables.ductTableName);
            sqliteHelper.ClearTable(ThTCHCommonTables.interfaceTableName);
            foreach (var seg in segInfos)
            {
                RecordDuctInfo(ref gId);
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
                    centerPoint = l.StartPoint.TransformBy(mat)
                };
                var eEndParam = new TCHInterfaceParam()
                {
                    ID = ductParam.endFaceID,
                    sectionType = ductParam.sectionType,
                    height = height,
                    width = width,
                    normalVector = -dirVec,
                    heighVector = new Vector3d(0, 0, 1),
                    centerPoint = l.EndPoint.TransformBy(mat)
                };
                RecordPortInfo(sEndParam, eEndParam);
            }
            sqliteHelper.db.Close();
        }
        private void RecordDuctInfo(ref ulong gId)
        {
            ductParam = new TCHDuctParam()
            {
                ID = gId++,
                endFaceID = gId++,
                startFaceID = gId++,
                subSystemID = 3,
                materialID = 4,
                sectionType = 0,
                ductType = 1,
                Soft = 0,
                Bulge = 0.0,
                AirLoad = 10000.0
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
        private void RecordPortInfo(TCHInterfaceParam sEndParam, TCHInterfaceParam eEndParam)
        {
            string recordDuctSrtInfo = $"INSERT INTO " + ThTCHCommonTables.interfaceTableName +
                                 " VALUES ('" + sEndParam.ID.ToString() + "'," +
                                         "'" + sEndParam.sectionType.ToString() + "'," +
                                         "'" + sEndParam.height.ToString() + "'," +
                                         "'" + sEndParam.width.ToString() + "'," +
                                         "'" + CovertVector(sEndParam.normalVector) + "'," +
                                         "'" + CovertVector(sEndParam.heighVector) + "'," +
                                         "'" + CovertPoint(sEndParam.centerPoint) + "')";
            string recordDuctEndInfo = $"INSERT INTO " + ThTCHCommonTables.interfaceTableName +
                                 " VALUES ('" + eEndParam.ID.ToString() + "'," +
                                         "'" + eEndParam.sectionType.ToString() + "'," +
                                         "'" + eEndParam.height.ToString() + "'," +
                                         "'" + eEndParam.width.ToString() + "'," +
                                         "'" + CovertVector(eEndParam.normalVector) + "'," +
                                         "'" + CovertVector(eEndParam.heighVector) + "'," +
                                         "'" + CovertPoint(eEndParam.centerPoint) + "')";
            sqliteHelper.Query<TCHInterfaceParam>(recordDuctSrtInfo);
            sqliteHelper.Query<TCHInterfaceParam>(recordDuctEndInfo);
        }
        private static void GetWidthAndHeight(string size, out double width, out double height)
        {
            string[] s = size.Split('x');
            if (s.Length != 2)
                throw new NotImplementedException("Duct size info doesn't contain width or height");
            width = Double.Parse(s[0]);
            height = Double.Parse(s[1]);
        }
        private string CovertPoint(Point3d p)
        {
            return CovertVector(p.GetAsVector());
        }
        private string CovertVector(Vector3d v)
        {
            return $@"{{""X"":{Math.Round(v.X, 6)},""Y"":{Math.Round(v.Y, 6)},""Z"":{Math.Round(v.Z, 6)}}}";
        }
    }
}
