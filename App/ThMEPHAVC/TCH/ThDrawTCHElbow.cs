using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.TCH
{
    public class ThDrawTCHElbow
    {
        private TCHElbowParam elbowParam;
        private ThSQLiteHelper sqliteHelper;
        private ThDrawTCHFlanges flangesService;

        public ThDrawTCHElbow(ThSQLiteHelper sqliteHelper)
        {
            this.sqliteHelper = sqliteHelper;
            flangesService = new ThDrawTCHFlanges(sqliteHelper);
        }

        public void Draw(EntityModifyParam info, Matrix3d mat, ref ulong gId)
        {
            sqliteHelper.Conn();
            RecordElbowInfo(ref gId);
            GetElbowInfo(info, mat, out Point3d srtP, out Point3d endP, out Vector3d srtVec, out Vector3d endVec, out double elbowW);
            var gap = ThTCHCommonTables.flgThickness * 0.5;
            var sEndParam = new TCHInterfaceParam()
            {
                ID = elbowParam.startFaceID,
                sectionType = 0,
                height = 0,
                width = elbowW,
                normalVector = srtVec,
                heighVector = new Vector3d(0, 0, 1),
                centerPoint = srtP - (gap * srtVec)
            };
            var eEndParam = new TCHInterfaceParam()
            {
                ID = elbowParam.endFaceID,
                sectionType = 0,
                height = 0,
                width = elbowW,
                normalVector = endVec,
                heighVector = new Vector3d(0, 0, 1),
                centerPoint = endP - (gap * endVec)
            };
            flangesService.Draw(srtP, sEndParam.normalVector, elbowW, ref gId);
            flangesService.Draw(endP, eEndParam.normalVector, elbowW, ref gId);
            ThTCHService.RecordPortInfo(sqliteHelper, new List<TCHInterfaceParam>() { sEndParam, eEndParam });
            sqliteHelper.db.Close();
        }

        private void GetElbowInfo(EntityModifyParam info,
                                  Matrix3d mat,
                                  out Point3d srtP, 
                                  out Point3d endP,
                                  out Vector3d srtVec,
                                  out Vector3d endVec,
                                  out double elbowW)
        {
            var srtInfo = info.portWidths.FirstOrDefault();
            var endInfo = info.portWidths.LastOrDefault();
            var sP = srtInfo.Key;
            var eP = endInfo.Key;
            srtVec = (sP - info.centerP).GetNormal();
            endVec = (eP - info.centerP).GetNormal();
            var openAngle = srtVec.GetAngleTo(endVec);
            elbowW = Math.Min(srtInfo.Value, endInfo.Value);
            var shrinkLen =  ThDuctPortsShapeService.GetElbowShrink(openAngle, elbowW);
            srtP = (info.centerP + srtVec * shrinkLen).TransformBy(mat);
            endP = (info.centerP + endVec * shrinkLen).TransformBy(mat);
        }

        private void RecordElbowInfo(ref ulong gId)
        {
            elbowParam = new TCHElbowParam()
            {
                ID = gId++,
                endFaceID = gId++,
                startFaceID = gId++,
                subSystemID = 1,
                materialID = 0,
                type = 1,
                radRatio = 0.8,
                segments = 2
            };
            string recordDuct = $"INSERT INTO " + ThTCHCommonTables.elbowTableName +
                          " VALUES ('" + elbowParam.ID.ToString() + "'," +
                                  "'" + elbowParam.startFaceID.ToString() + "'," +
                                  "'" + elbowParam.endFaceID.ToString() + "'," +
                                  "'" + elbowParam.subSystemID.ToString() + "'," +
                                  "'" + elbowParam.materialID.ToString() + "'," +
                                  "'" + elbowParam.type.ToString() + "'," +
                                  "'" + elbowParam.radRatio.ToString() + "'," +
                                  "'" + elbowParam.segments.ToString() + "')";
            sqliteHelper.Query<TCHElbowParam>(recordDuct);
        }
    }
}