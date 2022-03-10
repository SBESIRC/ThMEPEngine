using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.TCH
{
    public class ThDrawTCHReducing
    {
        private ulong subSysId;
        private ThSQLiteHelper sqliteHelper;
        private TCHReducingParam reducingParam;
        private ThDrawTCHFlanges flangesService;

        public ThDrawTCHReducing(ThSQLiteHelper sqliteHelper, ulong subSysId)
        {
            this.subSysId = subSysId;
            this.sqliteHelper = sqliteHelper;
            flangesService = new ThDrawTCHFlanges(sqliteHelper);
        }

        public void Draw(List<LineGeoInfo> reducingInfos, Matrix3d mat, double mainHeight, double elevation, ref ulong gId)
        {
            sqliteHelper.Conn();
            var mmElevation = elevation * 1000;
            foreach (var reducing in reducingInfos)
            {
                RecordDuctInfo(ref gId);
                GetReducingInfo(reducing, mat, out Point3d srtP, out Point3d endP, out double bigWidth, out double smallWidth, out double bHeight, out double sHeight);
                var dirVec = (endP - srtP).GetNormal();
                var gap = ThTCHCommonTables.flgThickness * 0.5;
                var centerEleDisVec = ThMEPHVACService.GetEleDis(mmElevation, mainHeight, bHeight);
                var sEndParam = new TCHInterfaceParam()
                {
                    ID = reducingParam.startFaceID,
                    sectionType = 0,
                    height = bHeight,
                    width = bigWidth,
                    normalVector = -dirVec,
                    heighVector = new Vector3d(0, 0, 1),
                    centerPoint = srtP + (gap * dirVec) + centerEleDisVec
                };
                flangesService.Draw(srtP + centerEleDisVec, -sEndParam.normalVector, bigWidth, bHeight, ref gId);
                centerEleDisVec = ThMEPHVACService.GetEleDis(mmElevation, mainHeight, sHeight);
                var eEndParam = new TCHInterfaceParam()
                {
                    ID = reducingParam.endFaceID,
                    sectionType = 0,
                    height = sHeight,
                    width = smallWidth,
                    normalVector = dirVec,
                    heighVector = new Vector3d(0, 0, 1),
                    centerPoint = endP - (gap * dirVec) + centerEleDisVec
                };
                
                flangesService.Draw(endP + centerEleDisVec, -eEndParam.normalVector, smallWidth, sHeight, ref gId);
                ThTCHService.RecordPortInfo(sqliteHelper, new List<TCHInterfaceParam>() { sEndParam, eEndParam });
            }
            sqliteHelper.db.Close();
        }

        private void GetReducingInfo(LineGeoInfo reducing, 
                                     Matrix3d mat, 
                                     out Point3d srtP, 
                                     out Point3d endP, 
                                     out double bigWidth, 
                                     out double smallWidth,
                                     out double bHeight,
                                     out double sHeight)
        {
            var l = reducing.centerLines[0] as Line;
            srtP = new Point3d(l.StartPoint.X, l.StartPoint.Y, 0);
            bHeight = l.StartPoint.Z;
            sHeight = l.EndPoint.Z;
            endP = new Point3d(l.EndPoint.X, l.EndPoint.Y, 0);
            srtP = srtP.TransformBy(mat);
            endP = endP.TransformBy(mat);
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
                startFaceID = gId++,
                endFaceID = gId++,
                subSystemID = subSysId,
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