using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.TCH
{
    public class ThDrawTCHCross
    {
        private ulong subSysId;
        private TCHCrossParam crossParam;
        private ThSQLiteHelper sqliteHelper; 
        private ThDrawTCHFlanges flangesService;

        public ThDrawTCHCross(ThSQLiteHelper sqliteHelper, ulong subSysId)
        {
            this.subSysId = subSysId;
            this.sqliteHelper = sqliteHelper;
            flangesService = new ThDrawTCHFlanges(sqliteHelper);
        }

        public void Draw(EntityModifyParam info, Matrix3d mat, double mainHeight, double elevation, ref ulong gId)
        {
            sqliteHelper.Conn();
            var mmElevation = elevation * 1000;
            RecordCrossInfo(ref gId);
            GetCrossInfo(info, mat, out CrossInfo crossInfo, out Point3d mainBigP, out Point3d mainSmallP, out Point3d sideOutterP, out Point3d sideInnerP);
            var cP = info.centerP.TransformBy(mat);
            var gap = ThTCHCommonTables.flgThickness * 0.5;
            var v = (mainBigP - cP).GetNormal();
            ThMEPHVACService.GetWidthAndHeight(crossInfo.iWidth, out double iWidth, out double iHeight);
            var centerEleDisVec = ThMEPHVACService.GetEleDis(mmElevation, mainHeight, iHeight);
            var param1 = new TCHInterfaceParam()
            {
                ID = crossParam.mainFaceID,
                sectionType = 0,
                height = iHeight,
                width = iWidth,
                normalVector = v,
                heighVector = new Vector3d(0, 0, 1),
                centerPoint = mainBigP - v * gap + centerEleDisVec
            };
            flangesService.Draw(mainBigP + centerEleDisVec, param1.normalVector, iWidth, iHeight, ref gId);
            v = (mainSmallP - cP).GetNormal();
            ThMEPHVACService.GetWidthAndHeight(crossInfo.coWidth, out double coWidth, out double coHeight);
            centerEleDisVec = ThMEPHVACService.GetEleDis(mmElevation, mainHeight, coHeight);
            var param2 = new TCHInterfaceParam()
            {
                ID = crossParam.branchFaceID1,
                sectionType = 0,
                height = coHeight,
                width = coWidth,
                normalVector = v,
                heighVector = new Vector3d(0, 0, 1),
                centerPoint = mainSmallP - v * gap + centerEleDisVec
            };
            flangesService.Draw(mainSmallP + centerEleDisVec, param2.normalVector, coWidth, coHeight, ref gId);
            v = (sideInnerP - cP).GetNormal();
            ThMEPHVACService.GetWidthAndHeight(crossInfo.innerWidth, out double innerWidth, out double innerHeight);
            centerEleDisVec = ThMEPHVACService.GetEleDis(mmElevation, mainHeight, innerHeight);
            var param3 = new TCHInterfaceParam()
            {
                ID = crossParam.branchFaceID2,
                sectionType = 0,
                height = innerHeight,
                width = innerWidth,
                normalVector = v,
                heighVector = new Vector3d(0, 0, 1),
                centerPoint = sideInnerP - v * gap + centerEleDisVec
            };
            flangesService.Draw(sideInnerP+ centerEleDisVec, param3.normalVector, innerWidth, innerHeight, ref gId);
            v = (sideOutterP - cP).GetNormal();
            ThMEPHVACService.GetWidthAndHeight(crossInfo.outterWidth, out double outterWidth, out double outterHeight);
            centerEleDisVec = ThMEPHVACService.GetEleDis(mmElevation, mainHeight, outterHeight);
            var param4 = new TCHInterfaceParam()
            {
                ID = crossParam.branchFaceID3,
                sectionType = 0,
                height = outterHeight,
                width = outterWidth,
                normalVector = v,
                heighVector = new Vector3d(0, 0, 1),
                centerPoint = sideOutterP - v * gap + centerEleDisVec
            };
            flangesService.Draw(sideOutterP + centerEleDisVec, param3.normalVector, outterWidth, outterHeight, ref gId);
            ThTCHService.RecordPortInfo(sqliteHelper, new List<TCHInterfaceParam>() { param1, param2, param3, param4 });
            sqliteHelper.db.Close();
        }
        private void GetCrossInfo(EntityModifyParam info, Matrix3d mat, out CrossInfo crossInfo, out Point3d mainBigP, out Point3d mainSmallP, out Point3d sideOutterP, out Point3d sideInnerP)
        {
            crossInfo = GetCrossInfo(info);
            var cross = ThDuctPortsFactory.CreateCross(ThMEPHVACService.GetWidth(crossInfo.iWidth), 
                                                       ThMEPHVACService.GetWidth(crossInfo.innerWidth), 
                                                       ThMEPHVACService.GetWidth(crossInfo.coWidth),
                                                       ThMEPHVACService.GetWidth(crossInfo.outterWidth));
            var transMat = mat * GetTransMat(crossInfo.trans);
            mainBigP = ThTCHService.GetMidPoint(cross.flg[0] as Line).TransformBy(transMat);
            mainSmallP = ThTCHService.GetMidPoint(cross.flg[1] as Line).TransformBy(transMat);
            sideOutterP = ThTCHService.GetMidPoint(cross.flg[2] as Line).TransformBy(transMat);
            sideInnerP = ThTCHService.GetMidPoint(cross.flg[3] as Line).TransformBy(transMat);
        }
        private static Matrix3d GetTransMat(TransInfo trans)
        {
            var p = new Point3d(trans.centerPoint.X, trans.centerPoint.Y, 0);
            var flipMat = trans.flip ? Matrix3d.Mirroring(new Line3d(Point3d.Origin, new Point3d(0, 1, 0))) : Matrix3d.Identity;
            var mat = Matrix3d.Displacement(p.GetAsVector()) *
                      Matrix3d.Rotation(trans.rotateAngle, -Vector3d.ZAxis, Point3d.Origin) * flipMat;
            return mat;
        }
        private CrossInfo GetCrossInfo(EntityModifyParam info)
        {
            var points = info.portWidths.Keys.ToList();
            var inVec = (points[0] - info.centerP).GetNormal();
            SepCrossIdx(inVec, points, info.centerP, out int collinearIdx, out int other1Idx, out int other2Idx);
            var branchVec = (points[other1Idx] - info.centerP).GetNormal();
            var flag = inVec.CrossProduct(branchVec).Z > 0;
            var innerIdx = flag ? other1Idx : other2Idx;
            var outterIdx = flag ? other2Idx : other1Idx;
            double innerWidth = ThMEPHVACService.GetWidth(info.portWidths[points[innerIdx]]);
            double outterWidth = ThMEPHVACService.GetWidth(info.portWidths[points[outterIdx]]);
            double rotateAngle = ThDuctPortsShapeService.GetCrossRotateAngle(inVec);
            var innerVec = (points[innerIdx] - info.centerP).GetNormal();
            var judgeVec = (Vector3d.XAxis).RotateBy(rotateAngle, -Vector3d.ZAxis);
            var tor = new Tolerance(1e-3, 1e-3);
            var flip = false;
            if (!judgeVec.IsEqualTo(innerVec, tor))
                flip = true;
            if (outterWidth < innerWidth)
                flip = true;
            var trans = new TransInfo() { rotateAngle = rotateAngle, centerPoint = info.centerP, flip = flip };
            if (flip)
            {
                var t = innerIdx;
                innerIdx = outterIdx;
                outterIdx = t;
            }
            return new CrossInfo() { iWidth = info.portWidths[points[0]], 
                                     innerWidth = info.portWidths[points[innerIdx]], 
                                     coWidth = info.portWidths[points[collinearIdx]], 
                                     outterWidth = info.portWidths[points[outterIdx]], trans = trans };
        }

        private void SepCrossIdx(Vector3d inVec,
                                 List<Point3d> points,
                                 Point3d centerP,
                                 out int collinearIdx,
                                 out int other1Idx,
                                 out int other2Idx)
        {
            collinearIdx = 1;
            other1Idx = 2;
            other2Idx = 3;
            for (int i = 1; i < 4; ++i)
            {
                var dirVec = (points[i] - centerP).GetNormal();
                if (ThMEPHVACService.IsCollinear(inVec, dirVec))
                    collinearIdx = i;
            }
            if (collinearIdx == 2)
            {
                other1Idx = 1; other2Idx = 3;
            }
            if (collinearIdx == 3)
            {
                other1Idx = 1; other2Idx = 2;
            }
        }

        private void RecordCrossInfo(ref ulong gId)
        {
            crossParam = new TCHCrossParam()
            {
                ID = gId++,
                mainFaceID = gId++,
                branchFaceID1 = gId++,
                branchFaceID2 = gId++,
                branchFaceID3 = gId++,
                subSystemID = subSysId,
                materialID = 0,
                type = 4,
                radRatio = ThTCHCommonTables.radRatio
            };
            string recordDuct = $"INSERT INTO " + ThTCHCommonTables.crossTableName +
                          " VALUES ('" + crossParam.ID.ToString() + "'," +
                                  "'" + crossParam.mainFaceID.ToString() + "'," +
                                  "'" + crossParam.branchFaceID1.ToString() + "'," +
                                  "'" + crossParam.branchFaceID2.ToString() + "'," +
                                  "'" + crossParam.branchFaceID3.ToString() + "'," +
                                  "'" + crossParam.subSystemID.ToString() + "'," +
                                  "'" + crossParam.materialID.ToString() + "'," +
                                  "'" + crossParam.type.ToString() + "'," +
                                  "'" + crossParam.radRatio.ToString() + "')";
            sqliteHelper.Query<TCHCrossParam>(recordDuct);
        }
    }
}