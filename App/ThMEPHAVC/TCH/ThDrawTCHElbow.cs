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
        private ulong subSysId;
        private TCHElbowParam elbowParam;
        private ThSQLiteHelper sqliteHelper;
        private ThDrawTCHFlanges flangesService;

        public ThDrawTCHElbow(ThSQLiteHelper sqliteHelper, ulong subSysId)
        {
            this.subSysId = subSysId;
            this.sqliteHelper = sqliteHelper;
            flangesService = new ThDrawTCHFlanges(sqliteHelper);
        }

        public void Draw(EntityModifyParam info, Matrix3d mat, double mainHeight, double elevation, ref ulong gId)
        {
            sqliteHelper.Conn();
            var mmElevation = elevation * 1000;
            RecordElbowInfo(ref gId);
            GetElbowInfo(info, mat, out Point3d srtP, out Point3d endP, out Vector3d srtVec, out Vector3d endVec, out string elbowW);
            var gap = ThTCHCommonTables.flgThickness * 0.5;
            ThMEPHVACService.GetWidthAndHeight(elbowW, out double w, out double h);
            var centerEleDisVec = ThMEPHVACService.GetEleDis(mmElevation, mainHeight, h);
            var sEndParam = new TCHInterfaceParam()
            {
                ID = elbowParam.startFaceID,
                sectionType = 0,
                height = h,
                width = w,
                normalVector = srtVec,
                heighVector = new Vector3d(0, 0, 1),
                centerPoint = srtP - (gap * srtVec) + centerEleDisVec
            };
            var eEndParam = new TCHInterfaceParam()
            {
                ID = elbowParam.endFaceID,
                sectionType = 0,
                height = h,
                width = w,
                normalVector = endVec,
                heighVector = new Vector3d(0, 0, 1),
                centerPoint = endP - (gap * endVec) + centerEleDisVec
            };
            flangesService.Draw(srtP + centerEleDisVec, sEndParam.normalVector, w, h, ref gId);
            flangesService.Draw(endP + centerEleDisVec, eEndParam.normalVector, w, h, ref gId);
            ThTCHService.RecordPortInfo(sqliteHelper, new List<TCHInterfaceParam>() { sEndParam, eEndParam });
            sqliteHelper.db.Close();
        }

        private void GetElbowInfo(EntityModifyParam info,
                                  Matrix3d mat,
                                  out Point3d srtP, 
                                  out Point3d endP,
                                  out Vector3d srtVec,
                                  out Vector3d endVec,
                                  out string elbowW)
        {
            var srtInfo = info.portWidths.FirstOrDefault();
            var endInfo = info.portWidths.LastOrDefault();
            var sP = srtInfo.Key;
            var eP = endInfo.Key;
            srtVec = (sP - info.centerP).GetNormal();
            endVec = (eP - info.centerP).GetNormal();
            var openAngle = srtVec.GetAngleTo(endVec);
            elbowW = ThMEPHVACService.GetWidth(srtInfo.Value) < ThMEPHVACService.GetWidth(endInfo.Value) ? srtInfo.Value : endInfo.Value;
            var shrinkLen =  ThDuctPortsShapeService.GetElbowShrink(openAngle, ThMEPHVACService.GetWidth(elbowW));
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
                subSystemID = subSysId,
                materialID = 0,
                type = 1,
                radRatio = ThTCHCommonTables.radRatio,
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